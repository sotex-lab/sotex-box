/** @type {import('tailwindcss').Config} */
const config = {
  content: [
    "./src/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        "sotex-black": {
          50: "#31343B",
          450: "#4B4E57",
          900: "#222429"
        },
        "sotex-purple": {
          50: "#AD01FD"
        },
        "sotex-green": {
          50: "#BCF068"
        },
        "sotex-yellow": {
          50: "#FFC907"
        },
        "sotex-blue": {
          50: "#0E6EFF"
        }

      },
    },
  },
  plugins: [],
};
export default config;