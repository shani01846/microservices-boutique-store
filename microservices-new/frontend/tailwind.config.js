/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './pages/**/*.{js,ts,jsx,tsx,mdx}',
    './components/**/*.{js,ts,jsx,tsx,mdx}',
    './app/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  theme: {
    extend: {
      colors: {
        noir: '#0a0a0a',
        ivory: '#f5f0eb',
        gold: '#b8965a',
        'gold-light': '#d4b483',
        'warm-white': '#faf8f5',
        'stone': '#8c8279',
      },
      fontFamily: {
        serif: ['var(--font-cormorant)', 'Georgia', 'serif'],
        sans: ['var(--font-jost)', 'system-ui', 'sans-serif'],
      },
      letterSpacing: {
        'widest-xl': '0.3em',
      },
    },
  },
  plugins: [],
}