FROM mcr.microsoft.com/dotnet/aspnet:5.0

COPY bin/publish/ App/
WORKDIR /App
ENV DOTNET_RUNNING_IN_CONTAINER=true \
  ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "IssueReadApis.dll"]