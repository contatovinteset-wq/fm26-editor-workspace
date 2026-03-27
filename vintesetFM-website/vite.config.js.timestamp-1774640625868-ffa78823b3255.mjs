// vite.config.js
import { defineConfig, loadEnv } from "file:///E:/fm26-editor-workspace-main/fm26-editor-workspace/vintesetFM-website/node_modules/vite/dist/node/index.js";
import react from "file:///E:/fm26-editor-workspace-main/fm26-editor-workspace/vintesetFM-website/node_modules/@vitejs/plugin-react/dist/index.js";
import tailwindcss from "file:///E:/fm26-editor-workspace-main/fm26-editor-workspace/vintesetFM-website/node_modules/@tailwindcss/vite/dist/index.mjs";
var vite_config_default = defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  return {
    define: {
      "process.env.VITE_TWITCH_CLIENT_ID": JSON.stringify(env.VITE_TWITCH_CLIENT_ID),
      "process.env.VITE_TWITCH_APP_ACCESS_TOKEN": JSON.stringify(env.VITE_TWITCH_APP_ACCESS_TOKEN),
      "process.env.VITE_YOUTUBE_API_KEY": JSON.stringify(env.VITE_YOUTUBE_API_KEY)
    },
    plugins: [
      react(),
      tailwindcss()
    ],
    server: {
      proxy: {
        "/api": {
          target: "http://localhost:3000",
          changeOrigin: true,
          secure: false
        }
      }
    }
  };
});
export {
  vite_config_default as default
};
//# sourceMappingURL=data:application/json;base64,ewogICJ2ZXJzaW9uIjogMywKICAic291cmNlcyI6IFsidml0ZS5jb25maWcuanMiXSwKICAic291cmNlc0NvbnRlbnQiOiBbImNvbnN0IF9fdml0ZV9pbmplY3RlZF9vcmlnaW5hbF9kaXJuYW1lID0gXCJFOlxcXFxmbTI2LWVkaXRvci13b3Jrc3BhY2UtbWFpblxcXFxmbTI2LWVkaXRvci13b3Jrc3BhY2VcXFxcdmludGVzZXRGTS13ZWJzaXRlXCI7Y29uc3QgX192aXRlX2luamVjdGVkX29yaWdpbmFsX2ZpbGVuYW1lID0gXCJFOlxcXFxmbTI2LWVkaXRvci13b3Jrc3BhY2UtbWFpblxcXFxmbTI2LWVkaXRvci13b3Jrc3BhY2VcXFxcdmludGVzZXRGTS13ZWJzaXRlXFxcXHZpdGUuY29uZmlnLmpzXCI7Y29uc3QgX192aXRlX2luamVjdGVkX29yaWdpbmFsX2ltcG9ydF9tZXRhX3VybCA9IFwiZmlsZTovLy9FOi9mbTI2LWVkaXRvci13b3Jrc3BhY2UtbWFpbi9mbTI2LWVkaXRvci13b3Jrc3BhY2UvdmludGVzZXRGTS13ZWJzaXRlL3ZpdGUuY29uZmlnLmpzXCI7aW1wb3J0IHsgZGVmaW5lQ29uZmlnLCBsb2FkRW52IH0gZnJvbSAndml0ZSdcbmltcG9ydCByZWFjdCBmcm9tICdAdml0ZWpzL3BsdWdpbi1yZWFjdCdcbmltcG9ydCB0YWlsd2luZGNzcyBmcm9tICdAdGFpbHdpbmRjc3Mvdml0ZSdcblxuLy8gaHR0cHM6Ly92aXRlanMuZGV2L2NvbmZpZy9cbmV4cG9ydCBkZWZhdWx0IGRlZmluZUNvbmZpZygoeyBtb2RlIH0pID0+IHtcbiAgY29uc3QgZW52ID0gbG9hZEVudihtb2RlLCBwcm9jZXNzLmN3ZCgpLCAnJyk7XG4gIHJldHVybiB7XG4gICAgZGVmaW5lOiB7XG4gICAgICAncHJvY2Vzcy5lbnYuVklURV9UV0lUQ0hfQ0xJRU5UX0lEJzogSlNPTi5zdHJpbmdpZnkoZW52LlZJVEVfVFdJVENIX0NMSUVOVF9JRCksXG4gICAgICAncHJvY2Vzcy5lbnYuVklURV9UV0lUQ0hfQVBQX0FDQ0VTU19UT0tFTic6IEpTT04uc3RyaW5naWZ5KGVudi5WSVRFX1RXSVRDSF9BUFBfQUNDRVNTX1RPS0VOKSxcbiAgICAgICdwcm9jZXNzLmVudi5WSVRFX1lPVVRVQkVfQVBJX0tFWSc6IEpTT04uc3RyaW5naWZ5KGVudi5WSVRFX1lPVVRVQkVfQVBJX0tFWSlcbiAgICB9LFxuICAgIHBsdWdpbnM6IFtcbiAgICAgIHJlYWN0KCksXG4gICAgICB0YWlsd2luZGNzcygpLFxuICAgIF0sXG4gICAgc2VydmVyOiB7XG4gICAgICBwcm94eToge1xuICAgICAgICAnL2FwaSc6IHtcbiAgICAgICAgICB0YXJnZXQ6ICdodHRwOi8vbG9jYWxob3N0OjMwMDAnLFxuICAgICAgICAgIGNoYW5nZU9yaWdpbjogdHJ1ZSxcbiAgICAgICAgICBzZWN1cmU6IGZhbHNlLFxuICAgICAgICB9XG4gICAgICB9XG4gICAgfVxuICB9XG59KVxuIl0sCiAgIm1hcHBpbmdzIjogIjtBQUE0WSxTQUFTLGNBQWMsZUFBZTtBQUNsYixPQUFPLFdBQVc7QUFDbEIsT0FBTyxpQkFBaUI7QUFHeEIsSUFBTyxzQkFBUSxhQUFhLENBQUMsRUFBRSxLQUFLLE1BQU07QUFDeEMsUUFBTSxNQUFNLFFBQVEsTUFBTSxRQUFRLElBQUksR0FBRyxFQUFFO0FBQzNDLFNBQU87QUFBQSxJQUNMLFFBQVE7QUFBQSxNQUNOLHFDQUFxQyxLQUFLLFVBQVUsSUFBSSxxQkFBcUI7QUFBQSxNQUM3RSw0Q0FBNEMsS0FBSyxVQUFVLElBQUksNEJBQTRCO0FBQUEsTUFDM0Ysb0NBQW9DLEtBQUssVUFBVSxJQUFJLG9CQUFvQjtBQUFBLElBQzdFO0FBQUEsSUFDQSxTQUFTO0FBQUEsTUFDUCxNQUFNO0FBQUEsTUFDTixZQUFZO0FBQUEsSUFDZDtBQUFBLElBQ0EsUUFBUTtBQUFBLE1BQ04sT0FBTztBQUFBLFFBQ0wsUUFBUTtBQUFBLFVBQ04sUUFBUTtBQUFBLFVBQ1IsY0FBYztBQUFBLFVBQ2QsUUFBUTtBQUFBLFFBQ1Y7QUFBQSxNQUNGO0FBQUEsSUFDRjtBQUFBLEVBQ0Y7QUFDRixDQUFDOyIsCiAgIm5hbWVzIjogW10KfQo=
