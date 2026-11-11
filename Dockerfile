FROM ://microsoft.com AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 80

FROM ://microsoft.com AS build
WORKDIR /src
COPY ["*.csproj", "./"]
RUN dotnet restore "./"
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Win.Forms.App.API.dll"]
