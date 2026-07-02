import type { Metadata } from 'next'
import './globals.css'
import { AuthProvider } from '../lib/auth'
import Header from '../components/Header'

export const metadata: Metadata = {
  title: 'MAISON — Designer Boutique',
  description: 'Exclusive designer clothing and accessories',
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className="bg-warm-white min-h-screen">
        <AuthProvider>
          <Header />
          <main className="container mx-auto px-6 py-12 max-w-7xl">
            {children}
          </main>
          <footer className="mt-24 border-t border-stone/20 py-12 text-center">
            <p className="font-serif text-2xl tracking-widest text-noir mb-3">MAISON</p>
            <div className="divider-gold w-24 mx-auto mb-4" />
            <p className="text-stone text-xs tracking-widest uppercase font-sans">
              © {new Date().getFullYear()} — All Rights Reserved
            </p>
          </footer>
        </AuthProvider>
      </body>
    </html>
  )
}
