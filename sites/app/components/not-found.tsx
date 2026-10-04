import { Button } from "@/components/ui/button"

/** The 404 page, as gpui-kit.com's: the code, a line and a way home. */
export function NotFound() {
  return (
    <main
      id="content"
      class="flex flex-1 flex-col items-center justify-center gap-4 px-6 py-24 text-center"
    >
      <p class="kicker">Error</p>
      <h1 class="text-[clamp(3rem,8vw,5rem)] leading-none font-[660] tracking-[-0.05em]">
        404
      </h1>
      <p class="text-muted-foreground">This page could not be found.</p>
      <div class="mt-4 flex gap-3">
        <Button
          class="h-10 rounded-[var(--radius-control)] px-4 font-semibold"
          render={<a href="/" />}
        >
          Go home
        </Button>
        <Button
          variant="outline"
          class="h-10 rounded-[var(--radius-control)] px-4 font-semibold"
          render={<a href="/docs" />}
        >
          Read the docs
        </Button>
      </div>
    </main>
  )
}
