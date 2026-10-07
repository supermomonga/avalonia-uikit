import { createRoute } from "honox/factory"
import { IconsPage, iconsDescription } from "@/components/icons-page"

export default createRoute((c) =>
  c.render(<IconsPage />, { title: "Icons", description: iconsDescription })
)
