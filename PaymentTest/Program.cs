
using PaymentTest.Controllers;
using TbpEcr;
using TbpEcr.Transport;

var builder = WebApplication.CreateBuilder(args);

// Add logging configuration
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<TbpEcr.Transport.IEcrTransport>(sp =>
{
    return new TbpEcr.Transport.EcrTransportCOM("COM3"); // Replace with the actual COM port name
});
builder.Services.AddSingleton<TbpEcrConnector>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
