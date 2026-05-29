/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Montserrat', 'sans-serif'],
        montserrat: ['Montserrat', 'sans-serif'],
      },
      fontSize: {
        'xs': '12px',
        'sm': '13px',
        'md': '14px',
        'lg': '15px',
        'xl': '16px',
        '2xl': '20px',
        '3xl': '31px',
      },
      colors: {
        brand: {
          primary: '#1E40AF',
          tertiary: '#4469af',
          dark: '#212b36',
          light: '#f4f6f8',
          base: '#000000',
        },
      },
      borderRadius: {
        'xs': '6px',
        'sm': '8px',
        'md': '12px',
        'lg': '50px',
      },
      transitionDuration: {
        'instant': '150ms',
        'fast': '200ms',
      },
    },
  },
  plugins: [],
}
