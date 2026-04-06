//using Microsoft.AspNetCore.RateLimiting;
//using EmpPortal.Controllers.DropdownService;
//using EmpPortal.Controllers.Globalvariable;
//using EmpPortal.Dbconnection;
//using EmpPortal.DbHelper;
//using EmpPortal.GlobalErrorHandlingMiddleware;
//using EmpPortal.Helpers;
//using EmpPortal.LogService;
//using EmpPortal.ModuleService;
//using EmpPortal.Services;

//var builder = WebApplication.CreateBuilder(args);

//// Add services
//builder.Services.AddControllersWithViews();
////builder.Services.AddHttpContextAccessor();

////builder.Services.AddDistributedMemoryCache();

//builder.Services.AddSingleton<DataBaseConnection>();
//builder.Services.AddHttpContextAccessor();
//builder.Services.AddScoped<DbHelper>();
//builder.Services.AddSingleton<GlobalVariableService>();
//builder.Services.AddSingleton<DropdownService>();
//builder.Services.AddScoped<ModuleService>();
//builder.Services.AddScoped<LogService>();
//builder.Services.AddSingleton<ErrorLoggerService>();
//builder.Services.AddScoped<IMasterDataService, MasterDataService>();

//builder.Services.Configure<EncryptionSettings>(
//    builder.Configuration.GetSection("EncryptionSettings"));
//builder.Services.AddScoped<EncryptionHelper>();


//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromMinutes(180);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//});


//builder.Services.AddRateLimiter(options =>
//{
//    options.AddFixedWindowLimiter("LoginLimiter", limiter =>
//    {
//        limiter.PermitLimit = 2;              // Max attempts
//        limiter.Window = TimeSpan.FromMinutes(1);
//        limiter.QueueLimit = 0;
//    });

//    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
//});



//var app = builder.Build();

//// Use global error handling middleware
//app.UseMiddleware<GlobalErrorHandlingMiddleware>();

//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();
//app.UseSession();
//app.UseRateLimiter();

//app.UseMiddleware<SessionTimeoutMiddleware>();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=HomeLogin}/{action=Index}/{id?}");
//app.Run();

using EmpPortal.Helpers;
using Microsoft.AspNetCore.RateLimiting;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using travelexpensemanagement.GlobalErrorHandlingMiddleware;
using travelexpensemanagement.LogService;
using travelexpensemanagement.ModuleService;
using travelexpensemanagement.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<DataBaseConnection>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<DbHelper>();
builder.Services.AddScoped<DropdownService>();
builder.Services.AddScoped<ModuleService>();
builder.Services.AddScoped<LogService>();
builder.Services.AddScoped<ErrorLoggerService>();
builder.Services.AddScoped<IMasterDataService, MasterDataService>();

builder.Services.Configure<EncryptionSettings>(
builder.Configuration.GetSection("EncryptionSettings"));
builder.Services.AddScoped<EncryptionHelper>();





builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = "EmpPortal.Session";  // Set the cookie name //SumeshCode
    options.IdleTimeout = TimeSpan.FromMinutes(10); // session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;


    // If testing locally on HTTP, use None. On HTTPS, Always
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.None
        : CookieSecurePolicy.Always;
});


builder.Services.AddScoped<GlobalVariableService>();



builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LoginLimiter", limiter =>
    {
        limiter.PermitLimit = 2;              // Max attempts
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});



var app = builder.Build();

// Use global error handling middleware
app.UseMiddleware<GlobalErrorHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseRateLimiter();

app.UseMiddleware<SessionTimeoutMiddleware>();
app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=EmpLogin}/{action=Index}/{id?}");
app.Run();



