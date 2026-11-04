import { serve } from "bun";
import index from "./index.html";

// Only used by `bun dev`: the page runs on port 3000, so API calls go on to the server in Rider.
const forward = (req: Request) => fetch("http://localhost:5099" + new URL(req.url).pathname, req);

const server = serve({
  routes: {
    "/*": index,
    "/login": forward,
    "/api/*": forward,
  },

  development: process.env.NODE_ENV !== "production" && {
    hmr: true,
    console: true,
  },
});

console.log(`Server running at ${server.url}`);
