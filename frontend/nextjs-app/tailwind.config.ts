import type { Config } from 'tailwindcss';

const config: Config = {
  content: ['./src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        ink: {
          DEFAULT: '#16233a',
          50: '#f4f6fa', 100: '#e6eaf2', 200: '#c8d2e3',
          400: '#6e7f9d', 600: '#33456a', 700: '#243652', 900: '#0f1828',
        },
        accent: {
          DEFAULT: '#0f766e',
          soft: '#e6f4f1',
          strong: '#0b5c56',
        },
        surface: '#f7f8fa',
        line: '#e4e7ec',
      },
      fontFamily: {
        sans: ['var(--font-sans)', 'ui-sans-serif', 'system-ui', 'sans-serif'],
      },
      fontSize: {
        'data-lg': ['1.75rem', { lineHeight: '2rem', letterSpacing: '-0.02em' }],
      },
      boxShadow: {
        card: '0 1px 2px rgba(16, 24, 40, 0.06), 0 1px 3px rgba(16, 24, 40, 0.04)',
      },
    },
  },
  plugins: [],
};

export default config;
