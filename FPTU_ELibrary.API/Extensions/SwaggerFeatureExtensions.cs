namespace FPTU_ELibrary.API.Extensions
{
    public static class SwaggerFeatureExtensions
    {
        public static WebApplication WithSwagger(this WebApplication app)
        { 
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json",
                    "FPTU_ELibraryManagement API V1");
            });

            return app;
        }
    }
}
