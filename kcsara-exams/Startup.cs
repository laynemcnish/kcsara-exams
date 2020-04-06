using System;
using System.IdentityModel.Tokens.Jwt;
using Kcsara.Exams.Certificates;
using Kcsara.Exams.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SarData.Server.Apis;

namespace Kcsara.Exams
{
  public class Startup
  {
    private readonly IWebHostEnvironment env;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
      Configuration = configuration;
      this.env = env;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
      services.AddAuthentication(options => {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
      })
      .AddCookie()
      .AddOpenIdConnect(options =>
      {
        options.Authority = Configuration["auth:authority"];
        options.RequireHttpsMetadata = !env.IsDevelopment();
        options.ClientId = Configuration["auth:frontend:client_id"];
        options.ClientSecret = Configuration["auth:frontend:client_secret"];
        foreach (var scope in Configuration["auth:frontend:scope"]?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new string[0])
        {
          options.Scope.Add(scope);
        }
        options.ResponseType = "code";
        options.GetClaimsFromUserInfoEndpoint = true;
        options.ClaimActions.MapUniqueJsonKey("memberId", "memberId");
        options.SaveTokens = true;
      });
      services.AddControllersWithViews();
      services.AddRazorPages();

      services.AddTableStorage(Configuration);
      services.AddSingleton<CertificateStore>();
      services.AddSingleton(QuizStore.init(Configuration["local_files"]));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }
      app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllerRoute(
                  name: "default",
                  pattern: "{controller=Home}/{action=Index}/{id?}");
        endpoints.MapRazorPages();
      });
    }
  }
}
