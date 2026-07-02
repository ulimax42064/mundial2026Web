using TUPMundial.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<MundialService>(); // ← registra el servicio
builder.Services.AddSession(options =>           // ← habilita sesiones
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        });

        var app = builder.Build();

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();  // ← importante: antes de MapControllerRoute
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
                pattern: "{controller=Auth}/{action=Login}/{id?}"); // ← arranca en Login

                app.Run();
