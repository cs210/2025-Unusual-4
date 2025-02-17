/** @type {import('next').NextConfig} */
const nextConfig = {
  // experimental: {
  //   serverActions: true,
  // },
  typescript: {
    ignoreBuildErrors: false,
  },
  // Add configuration to disable static generation for chat page
  experimental: {
    // Enable server actions if needed
    // serverActions: true,
  },
  // Configure the chat page to be dynamic
  // unstable_runtimeJS: true,
  async headers() {
    return [
      {
        source: '/chat',
        headers: [
          {
            key: 'Cache-Control',
            value: 'no-store',
          },
        ],
      },
    ]
  },
};

module.exports = nextConfig; 