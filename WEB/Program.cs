var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation(); // Hot reload cho Views

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure HttpClient để gọi Backend API
var backendBaseUrl = builder.Configuration["BackendAPI:BaseUrl"] ?? "https://localhost:7000/api/";
builder.Services.AddHttpClient("BackendAPI", client =>
{
    client.BaseAddress = new Uri(backendBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register Services
builder.Services.AddScoped<WEB.Services.ApiService>();
builder.Services.AddScoped<WEB.Services.ProductService>();
builder.Services.AddScoped<WEB.Services.AuthService>();
builder.Services.AddScoped<WEB.Services.ComplaintService>();

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
