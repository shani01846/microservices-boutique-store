'use client'

import Link from 'next/link'
import { useAuth } from '../lib/auth'
import { ShoppingBag, User, LogOut, Settings } from 'lucide-react'
import { motion, AnimatePresence } from 'framer-motion'
import { useState, useEffect } from 'react'

export default function Header() {
  const { user, logout, isAdmin } = useAuth()
  const [scrolled, setScrolled] = useState(false)
  const [menuOpen, setMenuOpen] = useState(false)

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 20)
    window.addEventListener('scroll', onScroll)
    return () => window.removeEventListener('scroll', onScroll)
  }, [])

  return (
    <motion.header
      initial={{ y: -80, opacity: 0 }}
      animate={{ y: 0, opacity: 1 }}
      transition={{ duration: 0.8, ease: [0.22, 1, 0.36, 1] }}
      className={`sticky top-0 z-50 transition-all duration-500 ${
        scrolled ? 'bg-warm-white/95 backdrop-blur-sm shadow-sm' : 'bg-warm-white'
      }`}
    >
      {/* Top announcement bar */}
      <div className="bg-noir text-ivory text-center py-2">
        <p className="text-xs tracking-widest-xl uppercase font-sans font-light">
          Complimentary shipping on orders over $500
        </p>
      </div>

      <div className="container mx-auto px-6 max-w-7xl">
        <div className="flex justify-between items-center py-5">
          {/* Left nav */}
          <nav className="hidden md:flex items-center gap-8">
            <Link href="/" className="text-xs tracking-widest uppercase font-sans text-stone hover:text-noir transition-colors duration-300">
              Collection
            </Link>
            {user && (
              <Link href="/orders" className="text-xs tracking-widest uppercase font-sans text-stone hover:text-noir transition-colors duration-300">
                Orders
              </Link>
            )}
            {isAdmin() && (
              <Link href="/admin" className="text-xs tracking-widest uppercase font-sans text-stone hover:text-noir transition-colors duration-300">
                Atelier
              </Link>
            )}
          </nav>

          {/* Center logo */}
          <Link href="/" className="absolute left-1/2 -translate-x-1/2">
            <motion.div whileHover={{ opacity: 0.7 }} transition={{ duration: 0.3 }}>
              <h1 className="font-serif text-3xl tracking-widest-xl text-noir font-light">MAISON</h1>
            </motion.div>
          </Link>

          {/* Right actions */}
          <div className="flex items-center gap-5 ml-auto">
            {user ? (
              <>
                <span className="hidden md:block text-xs tracking-widest uppercase font-sans text-stone">
                  {user.firstName}
                </span>
                <Link href="/cart" className="relative group">
                  <ShoppingBag size={18} className="text-noir group-hover:text-gold transition-colors duration-300" />
                </Link>
                <button onClick={logout} className="group">
                  <LogOut size={16} className="text-stone group-hover:text-noir transition-colors duration-300" />
                </button>
              </>
            ) : (
              <div className="flex items-center gap-4">
                <Link href="/auth/login" className="text-xs tracking-widest uppercase font-sans text-stone hover:text-noir transition-colors duration-300">
                  Sign In
                </Link>
                <Link
                  href="/auth/register"
                  className="text-xs tracking-widest uppercase font-sans border border-noir text-noir px-4 py-2 hover:bg-noir hover:text-ivory transition-all duration-300"
                >
                  Register
                </Link>
              </div>
            )}
          </div>
        </div>

        {/* Gold divider */}
        <div className="divider-gold" />
      </div>
    </motion.header>
  )
}
