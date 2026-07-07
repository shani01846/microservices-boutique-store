'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { useAuth } from '../../../lib/auth'
import { motion } from 'framer-motion'

export default function Login() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const { login } = useAuth()
  const router = useRouter()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    const success = await login(email, password)
    if (success) router.push('/')
    else setError('Invalid credentials. Please try again.')
    setLoading(false)
  }

  return (
    <div className="min-h-[70vh] flex items-center justify-center">
      <motion.div
        initial={{ opacity: 0, y: 30 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.7, ease: [0.22, 1, 0.36, 1] }}
        className="w-full max-w-md"
      >
        <div className="text-center mb-10">
          <p className="text-xs tracking-widest-xl uppercase font-sans text-gold mb-3">Welcome Back</p>
          <h1 className="font-serif text-4xl font-light text-noir mb-4">Sign In</h1>
          <div className="divider-gold w-16 mx-auto" />
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label className="block text-xs tracking-widest uppercase font-sans text-stone mb-2">Email</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full px-0 py-3 border-0 border-b border-stone/30 bg-transparent font-sans text-sm text-noir focus:border-gold transition-colors duration-300"
              required
            />
          </div>

          <div>
            <label className="block text-xs tracking-widest uppercase font-sans text-stone mb-2">Password</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-0 py-3 border-0 border-b border-stone/30 bg-transparent font-sans text-sm text-noir focus:border-gold transition-colors duration-300"
              required
            />
          </div>

          {error && (
            <motion.p initial={{ opacity: 0 }} animate={{ opacity: 1 }}
              className="text-xs tracking-widest font-sans text-red-400 text-center">
              {error}
            </motion.p>
          )}

          <div className="pt-4">
            <button
              type="submit"
              disabled={loading}
              className="w-full bg-noir text-ivory py-4 text-xs tracking-widest uppercase font-sans hover:bg-stone transition-colors duration-300 disabled:opacity-50"
            >
              {loading ? 'Signing In...' : 'Sign In'}
            </button>
          </div>
        </form>

        <p className="text-center mt-8 text-xs tracking-widest font-sans text-stone">
          New to VÈRA?{' '}
          <Link href="/auth/register" className="text-noir hover:text-gold transition-colors duration-300 underline underline-offset-4">
            Create an account
          </Link>
        </p>
      </motion.div>
    </div>
  )
}
