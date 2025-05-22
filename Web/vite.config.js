import { sveltekit } from "@sveltejs/kit/vite";
import tailwindcss from "@tailwindcss/vite";

/** @type {import('vite').UserConfig} */
export default {
  plugins: [sveltekit(), tailwindcss()],
};
