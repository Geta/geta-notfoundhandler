using Geta.NotFoundHandler.Web;

Foundation.Program.CreateHostBuilder<Startup>(args,
                                              webBuilder =>
                                                  webBuilder.UseContentRoot(
                                                      Path.GetFullPath(
                                                          "../../sub/geta-foundation-core/src/Foundation")))
    .Build()
    .Run();
