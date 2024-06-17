FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.19
WORKDIR /workspace
RUN apk --no-cache add bash curl docker-cli-buildx git jq make gcompat
COPY --from=ngrok/ngrok:latest /bin/ngrok /bin/ngrok