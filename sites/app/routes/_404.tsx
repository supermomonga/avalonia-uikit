import type { NotFoundHandler } from "hono"
import {
  PageActions,
  PageHeader,
  PageHeaderDescription,
  PageHeaderHeading,
} from "@/components/page-header"
import { Button } from "@/components/ui/button"

const handler: NotFoundHandler = (c) => {
  c.status(404)
  return c.render(
    <PageHeader>
      <PageHeaderHeading>404</PageHeaderHeading>
      <PageHeaderDescription>
        This page could not be found.
      </PageHeaderDescription>
      <PageActions>
        <Button render={<a href="/" />}>Go home</Button>
      </PageActions>
    </PageHeader>,
    { title: "Not Found" }
  )
}

export default handler
