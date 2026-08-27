import { expect, test } from "@playwright/test";

test("a new developer can stream a simulated response without configuring credentials", async ({ page }, testInfo) => {
  await page.goto("/chat");

  await expect(page.getByRole("heading", { name: "AI Agent Chat" })).toBeVisible();

  // The heading is server-prerendered; wait for the WebAssembly runtime before
  // dispatching the input event that drives the interactive component state.
  await page.waitForTimeout(750);

  const input = page.getByPlaceholder("Type a message...");
  await input.fill("Show that this demo streams safely.");
  await page.getByRole("button", { name: "Send", exact: true }).click();

  await expect(page.getByRole("button", { name: "Stop", exact: true })).toBeVisible();
  await expect(page.locator(".message.assistant")).toContainText(
    "I am a simulated AI agent. You said: Show that this demo streams safely. This streams token by token!");
  await expect(page.getByRole("button", { name: "Stop", exact: true })).toBeHidden();

  await testInfo.attach("successful-chat", {
    body: await page.screenshot({ fullPage: true }),
    contentType: "image/png"
  });
});
