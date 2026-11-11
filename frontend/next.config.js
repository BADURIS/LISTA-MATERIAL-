/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  // O navegador chama /api no mesmo endereço do site e o Next repassa ao backend (LMaterial.API).
  // Assim celulares e notebooks da rede só precisam acessar o endereço do frontend.
  async rewrites() {
    const backend = process.env.API_URL || 'http://localhost:5175';
    return [{ source: '/api/:path*', destination: `${backend}/api/:path*` }];
  },
};

module.exports = nextConfig;
