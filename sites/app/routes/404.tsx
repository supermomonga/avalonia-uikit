import { createRoute } from "honox/factory"
import {
  PageActions,
  PageHeader,
  PageHeaderDescription,
  PageHeaderHeading,
} from "@/components/page-header"
import { Button } from "@/components/ui/button"

export default createRoute((c) =>
  c.render(
    <PageHeader>
      <PageHeaderHeading>404</PageHeaderHeading>
      <PageHeaderDescription>
        This page could not be found.
      </PageHeaderDescription>
      <PageActions>
        <Button render={<a href="/" />}>Go home</Button>
        <Button variant="ghost" render={<a href="/docs" />}>
          Read the docs
        </Button>
      </PageActions>
    </PageHeader>,
    { title: "Not Found" }
  )
)
