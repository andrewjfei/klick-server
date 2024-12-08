using KlickServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// redirect http requests to https
app.UseHttpsRedirection();

// route requests to correct route handler
app.UseRouting();

// allow any origin to access server
app.UseCors(policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .WithOrigins("http://localhost:3000"));

app.MapHub<RoomHub>("/roomHub");

app.Run();
