import type { Metadata } from 'next'
import './globals.css'
import { AuthProvider } from '../lib/auth'
import Header from '../components/Header'

export const metadata: Metadata = {
  title: 'ECommerce Store',
  description: 'A modern clothing e-commerce store',
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en">
      <body className="bg-gray-50 min-h-screen">
        <AuthProvider>
          <Header />
          <main className="container mx-auto px-4 py-8">
            {children}
          </main>
        </AuthProvider>
      </body>
    </html>
  )
}