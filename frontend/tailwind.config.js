/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        normatel: {
          green: "#1F6D2E",
          "green-hover": "#175423",
          "green-light": "#EAF3DE",
          "green-border": "#C0DD97",
          "green-text": "#173404",
          amber: "#FAC775",
          "amber-text": "#412402",
          "red-badge": "#FAD2D2",
          "red-text": "#5C0606",
        },
      },
    },
  },
  plugins: [],
};
