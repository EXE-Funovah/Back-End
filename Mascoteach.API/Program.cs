using Mascoteach.Data.Interfaces;
using Mascoteach.Data.Models;
using Mascoteach.Data.Repositories;
using Mascoteach.Service.Implementations;
using Mascoteach.Service.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Amazon.S3;
using Amazon.Runtime;




var builder = WebApplication.CreateBuilder(args);

// Database Context 
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<MascoteachDbContext>(options =>
    options.UseSqlServer(connectionString));


// Repositories (Data Layer)
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IQuizRepository, QuizRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IOptionRepository, OptionRepository>();
builder.Services.AddScoped<IGameTemplateRepository, GameTemplateRepository>();
builder.Services.AddScoped<ILiveSessionRepository, LiveSessionRepository>();
builder.Services.AddScoped<ISessionParticipantRepository, SessionParticipantRepository>();



// Services (Business Layer)
builder.Services.AddAutoMapper(typeof(IAuthService).Assembly);
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IOptionService, OptionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGameTemplateService, GameTemplateService>();
builder.Services.AddScoped<ILiveSessionService, LiveSessionService>();
builder.Services.AddScoped<ISessionParticipantService, SessionParticipantService>();
builder.Services.AddScoped<IS3Service, S3Service>();

// AWS S3 Configuration
var awsAccessKey = builder.Configuration["AWS:AccessKey"];
var awsSecretKey = builder.Configuration["AWS:SecretKey"];
var awsRegion = builder.Configuration["AWS:Region"];

if (!string.IsNullOrEmpty(awsAccessKey) && !string.IsNullOrEmpty(awsSecretKey))
{
    var credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
    var s3Config = new AmazonS3Config
    {
        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsRegion ?? "us-east-1")
    };
    builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(credentials, s3Config));
}
else
{
    builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(awsRegion ?? "us-east-1")));
}

// HttpClient cho AI Service
builder.Services.AddHttpClient("AIService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AIService:BaseUrl"] ?? "http://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(120);
});

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mascoteach API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter Token: Bearer {your_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

// Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    
}

app.UseSwagger();

app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
