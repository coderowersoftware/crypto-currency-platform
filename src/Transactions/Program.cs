using CodeRower.CCP.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using Transactions.Facade;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

builder.Services.AddControllers().AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.Converters.Add(new StringEnumConverter());
    });

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Description = "Standard Authorization header using Bearer scheme.",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Scheme = "Bearer",
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSecret = builder.Configuration.GetSection("AppSettings:AuthJwtSecret").Value;
        options.TokenValidationParameters = new TokenValidationParameters
        {

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret ?? string.Empty)),
            RequireExpirationTime = false,
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false
        };
    });

// Add service dependencies
builder.Services.AddSingleton<IRestApiFacade, RestApiFacade>();
builder.Services.AddSingleton<ITransactionsService, TransactionsService>();
builder.Services.AddSingleton<IMiningService, MiningService>();
builder.Services.AddSingleton<IReportsService, ReportsService>();
builder.Services.AddSingleton<IUsersService, UsersService>();
builder.Services.AddSingleton<ICustomerService, CustomerService>();
builder.Services.AddSingleton<ITenantService, TenantService>();
builder.Services.AddSingleton<ISmsService, SmsService>();
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();

//Configure other services up here
var configurationOptions = new ConfigurationOptions
{
    EndPoints = { "redis-11871.c301.ap-south-1-1.ec2.cloud.redislabs.com:11871" },
    User = "dashboard-read-write",
    Password = "*5Hc95-um9U8Wxg",
   //Ssl = true,
    AbortOnConnectFail = false,
};


var multiplexer = ConnectionMultiplexer.Connect(configurationOptions); ;
//var multiplexer = ConnectionMultiplexer.Connect("redis://dashboard-read-write:*5Hc95-um9U8Wxg@redis-11871.c301.ap-south-1-1.ec2.cloud.redislabs.com:11871&abortConnect=false");
builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "tenant/{tenantId}/{controller=Home}/{action=Index}/{id?}");

app.Run();
