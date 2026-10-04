// A page that mounts every demo, for checking the bundle by hand
// (dotnet run / dotnet serve). The site has its own loader (sites/app/avalonia-demo.ts).
import { dotnet } from "./_framework/dotnet.js";

const status = document.getElementById("status");
const runtime = await dotnet.withApplicationArguments().create();
const config = runtime.getConfig();
const exports = await runtime.getAssemblyExports(config.mainAssemblyName);
await runtime.runMain(config.mainAssemblyName, []);
const api = exports.AvaloniaUIKit.Browser.Demos;

const ids = api.List();
status.textContent = `${ids.length} demos`;
const container = document.getElementById("demos");
for (const [index, id] of ids.entries()) {
  const section = document.createElement("section");
  section.className = "demo";
  const title = document.createElement("h2");
  title.textContent = id;
  const host = document.createElement("div");
  host.className = "host";
  host.id = `host-${index}`;
  section.append(title, host);
  container.append(section);
  // No inset: the host is the box itself, so a focus ring at its edge is cut off.
  const mounted = api.Mount(host.id, id, 0, (height) => {
    host.style.height = `${height}px`;
  });
  if (!mounted) title.textContent += " (failed)";
}

document.getElementById("theme").addEventListener("click", () => {
  const dark = document.body.classList.toggle("dark");
  api.SetTheme(dark);
});
