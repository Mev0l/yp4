using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Console;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTimeOffset PublicationDate { get; set; }
}

public class BlogDbContext : DbContext
{
    public DbSet<Category> Categories { get; set; }
    public DbSet<Post> Posts { get; set; }

    public BlogDbContext(DbContextOptions<BlogDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Post>()
            .Property(p => p.PublicationDate)
            .HasColumnType("timestamp with time zone");
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(@"C:\Users\Mevole\source\repos\ConsoleApp1\ConsoleApp1")
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");

                var host = CreateHostBuilder(args, connectionString).Build();

                using (var scope = host.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    if (services == null)
                    {
                        Console.WriteLine("Ошибка: сервисы не были созданы.");
                        return;
                    }

                    var context = services.GetRequiredService<BlogDbContext>();

                    context.Database.Migrate();

                    AddCategory(context, "New Category");
                    RemoveCategory(context, 1);
                    PrintAllCategories(context);

                    AddPost(context, "New Post Title", "New Post Content", DateTime.UtcNow);
                    RemovePost(context, 1);
                
                    PrintAllPosts(context);
                    var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
                    var endDate = new DateTimeOffset(2024, 5, 31, 23, 59, 59, TimeSpan.Zero);
                    var postsInPeriod = GetPostsInPeriod(context, startDate, endDate);
                    Console.WriteLine($"Посты, опубликованные с {startDate} по {endDate}:");
                    foreach (var post in postsInPeriod)
                    {
                        Console.WriteLine($"Id: {post.Id}, Title: {post.Title}, Content: {post.Content}, Publication Date: {post.PublicationDate}");
                    }
                }


                host.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Произошла ошибка:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
        static List<Post> GetPostsInPeriod(BlogDbContext context, DateTimeOffset startDate, DateTimeOffset endDate)
        {
            return context.Posts
                .Where(p => p.PublicationDate >= startDate && p.PublicationDate <= endDate)
                .ToList();
        }
        static void AddCategory(BlogDbContext context, string categoryName)
        {
            var category = new Category { Name = categoryName };
            context.Categories.Add(category);
            context.SaveChanges();
            Console.WriteLine("Категория успешно добавлена.");
        }

        static void RemoveCategory(BlogDbContext context, int categoryId)
        {
            var category = context.Categories.Find(categoryId);
            if (category != null)
            {
                context.Categories.Remove(category);
                context.SaveChanges();
            }
        }

        static void PrintAllCategories(BlogDbContext context)
        {
            var categories = context.Categories.ToList();
            Console.WriteLine("Список категорий:");
            foreach (var category in categories)
            {
                Console.WriteLine($"Id: {category.Id}, Name: {category.Name}");
            }
        }

        static void AddPost(BlogDbContext context, string title, string content, DateTimeOffset publicationDate)
        {
            var post = new Post { Title = title, Content = content, PublicationDate = publicationDate };
            context.Posts.Add(post);
            context.SaveChanges();
            Console.WriteLine("Пост успешно добавлен.");
        }

        static void RemovePost(BlogDbContext context, int postId)
        {
            var post = context.Posts.Find(postId);
            if (post != null)
            {
                context.Posts.Remove(post);
                context.SaveChanges();
            }
        }

        static void PrintAllPosts(BlogDbContext context)
        {
            var posts = context.Posts.ToList();
            Console.WriteLine("Список постов:");
            foreach (var post in posts)
            {
                Console.WriteLine($"Id: {post.Id}, Title: {post.Title}, Content: {post.Content}, Publication Date: {post.PublicationDate}");
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args, string connectionString) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole(options =>
                    {
                        options.LogToStandardErrorThreshold = LogLevel.Warning;
                    })
                    .AddFilter<ConsoleLoggerProvider>("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
                })
                .ConfigureServices((_, services) =>
                {
                    services.AddDbContext<BlogDbContext>(options =>
                        options.UseNpgsql(connectionString));
                });
    }
}