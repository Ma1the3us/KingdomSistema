using MeuProjetoMVC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IEnderecoService, EnderecoService>();
builder.Services.AddHttpClient<IFreteServices, FreteSerivce>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// necessário para usar Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromHours(1);
});

// necessário para o layout usar IHttpContextAccessor
builder.Services.AddHttpContextAccessor();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// habilita session antes de endpoints
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
