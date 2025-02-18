using Geta.NotFoundHandler.Web;

Foundation.Program.CreateHostBuilder<Startup>(args,
                                              webBuilder =>
                                                  webBuilder.UseContentRoot(
                                                      Path.GetFullPath(
                                                          "../../sandbox/geta-packages-foundation-sandbox/src/Foundation")))
    .Build()
    .Run();
