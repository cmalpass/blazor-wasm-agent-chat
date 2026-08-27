import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  timeout: 30_000,
  retries: process.env.CI ? 1 : 0,
  reporter: [["list"], ["html", { outputFolder: "playwright-report", open: "never" }]],
  use: {
    baseURL: "http://127.0.0.1:5089",
    screenshot: "only-on-failure",
    trace: "retain-on-failure"
  },
  webServer: {
    command: "dotnet run --project BlazorAiChat/BlazorAiChat/BlazorAiChat.csproj --launch-profile http",
    url: "http://127.0.0.1:5089/chat",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000
  }
});
