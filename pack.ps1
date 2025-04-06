dotnet build -c Release "./src/BlendInteractive.Blit/BlendInteractive.Blit.csproj" /p:ContinuousIntegrationBuild=true
dotnet pack  -c Release --no-build -o "./artifacts/packages" "./src/BlendInteractive.Blit/BlendInteractive.Blit.csproj"

dotnet build -c Release "./src/BlendInteractive.Blit.Optimizely/BlendInteractive.Blit.Optimizely.csproj" /p:UseLocalPackage=True /p:ContinuousIntegrationBuild=true
dotnet pack  -c Release --no-build -o "./artifacts/packages" "./src/BlendInteractive.Blit.Optimizely/BlendInteractive.Blit.Optimizely.csproj" /p:UseLocalPackage=True

dotnet build -c Release "./src/BlendInteractive.Blit.Optimizely.UI/BlendInteractive.Blit.Optimizely.UI.csproj" /p:UseLocalPackage=True /p:ContinuousIntegrationBuild=true
dotnet pack  -c Release --no-build -o "./artifacts/packages" "./src/BlendInteractive.Blit.Optimizely.UI/BlendInteractive.Blit.Optimizely.UI.csproj" /p:UseLocalPackage=True
