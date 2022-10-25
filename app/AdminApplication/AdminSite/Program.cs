using Amazon.Polly;
using Amazon.Rekognition;
using Amazon.S3;
using Amazon.Translate;
using DataAccess.Data;
using DataAccess.Repository.Implementation;
using DataAccess.Repository.Implementation.InventoryImplementation;
using DataAccess.Repository.Implementation.NotificationsImplementations;
using DataAccess.Repository.Implementation.OrderImplementations;
using DataAccess.Repository.Implementation.SearchImplementation;
using DataAccess.Repository.Implementation.WelcomePageImplementation;
using DataAccess.Repository.Interface;
using DataAccess.Repository.Interface.Implementations;
using DataAccess.Repository.Interface.InventoryInterface;
using DataAccess.Repository.Interface.NotificationsInterface;
using DataAccess.Repository.Interface.OrdersInterface;
using DataAccess.Repository.Interface.SearchImplementations;
using DataAccess.Repository.Interface.WelcomePageInterface;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddAWSService<IAmazonPolly>();
builder.Services.AddAWSService<IAmazonRekognition>();
builder.Services.AddAWSService<IAmazonTranslate>();
builder.Services.AddAutoMapper(typeof(ApplicationBuilder));
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddDbContext<ApplicationDbContext>(option => option.UseSqlServer(builder.Configuration.GetConnectionString("BookstoreDbDefaultConnection")));

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Configure Authentication
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    x.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(x =>
{
    x.ResponseType = builder.Configuration["Authentication:Cognito:ResponseType"];
    x.MetadataAddress = builder.Configuration["Authentication:Cognito:MetadataAddress"];
    x.ClientId = builder.Configuration["Authentication:Cognito:ClientId"];
    x.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "cognito:username"
    };
    x.Events = new OpenIdConnectEvents() { OnRedirectToIdentityProviderForSignOut = OnRedirectToIdentityProviderForSignOut };
});

//Configure DI
builder.Services.AddTransient<ISearchDatabaseCalls, SearchDatabaseCalls>();
builder.Services.AddTransient<IExpressionFunction, ExpressionFunction>();
builder.Services.AddTransient<IOrderDatabaseCalls, OrderDatabaseCalls>();
builder.Services.AddTransient<IInventory, Inventory>();
builder.Services.AddTransient<IRekognitionNPollyRepository, RekognitionNPollyRepository>();
builder.Services.AddTransient<ISearchRepository, SearchRepository>();
builder.Services.AddTransient<IOrderRepository, OrderRepository>();
builder.Services.AddTransient<IOrderDetailRepository, OrderDetailRepository>();
builder.Services.AddTransient<IOrderStatusRepository, OrderStatusRepository>();
builder.Services.AddTransient<INotifications, Notifications>();
builder.Services.AddTransient<ICustomAdminPage, CustomAdmin>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Configure AWS Logging
builder.Logging.AddAWSProvider();

// Add AWS Systems Manager 
builder.Configuration.AddSystemsManager("/BobsUsedBookAdminStore/", optional: true);
builder.Configuration.AddSystemsManager("/bookstoredb/", optional: true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Configure Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultControllerRoute();
app.MapRazorPages();

// Create the database
using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetService<ApplicationDbContext>()!.Database.EnsureCreatedAsync();
}

app.Run();

Task OnRedirectToIdentityProviderForSignOut(RedirectContext context)
{
    context.ProtocolMessage.Scope = "openid";
    context.ProtocolMessage.ResponseType = "code";

    var cognitoDomain = builder.Configuration["Authentication:Cognito:CognitoDomain"];

    var clientId = builder.Configuration["Authentication:Cognito:ClientId"];

    var logoutUrl = $"{context.Request.Scheme}://{context.Request.Host}{builder.Configuration["Authentication:Cognito:AppSignOutUrl"]}";

    context.ProtocolMessage.IssuerAddress = $"{cognitoDomain}/logout?client_id={clientId}&logout_uri={logoutUrl}";

    // delete cookies
    context.Properties.Items.Remove(CookieAuthenticationDefaults.AuthenticationScheme);
    
    // close openid session
    context.Properties.Items.Remove(OpenIdConnectDefaults.AuthenticationScheme);

    return Task.CompletedTask;
}