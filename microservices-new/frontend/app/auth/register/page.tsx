'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import emailjs from '@emailjs/browser'
import { useAuth } from '../../../lib/auth'
import { motion, AnimatePresence } from 'framer-motion'

const EMAILJS_SERVICE_ID = process.env.NEXT_PUBLIC_EMAILJS_SERVICE_ID!
const EMAILJS_TEMPLATE_ID = process.env.NEXT_PUBLIC_EMAILJS_TEMPLATE_ID!
const EMAILJS_PUBLIC_KEY = process.env.NEXT_PUBLIC_EMAILJS_PUBLIC_KEY!

export default function Register() {
  const [formData, setFormData] = useState({ email: '', password: '', firstName: '', lastName: '' })
  const [step, setStep] = useState<'form' | 'verify'>('form')
  const [verificationCode, setVerificationCode] = useState('')
  const [generatedCode, setGeneratedCode] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const { register } = useAuth()
  const router = useRouter()

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) =>
    setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setLoading(true)
    const code = Math.floor(100000 + Math.random() * 900000).toString()
    setGeneratedCode(code)
    try {
      await emailjs.send(EMAILJS_SERVICE_ID, EMAILJS_TEMPLATE_ID,
        { user_name: `${formData.firstName} ${formData.lastName}`, user_email: formData.email, verification_code: code },
        EMAILJS_PUBLIC_KEY)
      setStep('verify')
    } catch {
      setError('Failed to send verification email. Please try again.')
    }
    setLoading(false)
  }

  const handleVerify = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    if (verificationCode !== generatedCode) { setError('Invalid verification code'); return }
    setLoading(true)
    const result = await register(formData.email, formData.password, formData.firstName, formData.lastName)
    if (result.success) router.push('/')
    else setError(result.message || 'Registration failed. Please try again.')
    setLoading(false)
  }

  const inputClass = "w-full px-0 py-3 border-0 border-b border-stone/30 bg-transparent font-sans text-sm text-noir focus:border-gold transition-colors duration-300"
  const labelClass = "block text-xs tracking-widest uppercase font-sans text-stone mb-2"

  return (
    <div className="min-h-[70vh] flex items-center justify-center">
      <motion.div
        initial={{ opacity: 0, y: 30 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.7, ease: [0.22, 1, 0.36, 1] }}
        className="w-full max-w-md"
      >
        <div className="text-center mb-10">
          <p className="text-xs tracking-widest-xl uppercase font-sans text-gold mb-3">Join VÈRA</p>
          <h1 className="font-serif text-4xl font-light text-noir mb-4">Create Account</h1>
          <div className="divider-gold w-16 mx-auto" />
        </div>

        <AnimatePresence mode="wait">
          {step === 'form' ? (
            <motion.form key="form" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
              onSubmit={handleSubmit} className="space-y-6">
              <div className="grid grid-cols-2 gap-6">
                <div>
                  <label className={labelClass}>First Name</label>
                  <input type="text" name="firstName" value={formData.firstName} onChange={handleChange} className={inputClass} required />
                </div>
                <div>
                  <label className={labelClass}>Last Name</label>
                  <input type="text" name="lastName" value={formData.lastName} onChange={handleChange} className={inputClass} required />
                </div>
              </div>
              <div>
                <label className={labelClass}>Email</label>
                <input type="email" name="email" value={formData.email} onChange={handleChange} className={inputClass} required />
              </div>
              <div>
                <label className={labelClass}>Password</label>
                <input type="password" name="password" value={formData.password} onChange={handleChange} className={inputClass} required />
              </div>

              {error && <p className="text-xs tracking-widest font-sans text-red-400 text-center">{error}</p>}

              <div className="pt-4">
                <button type="submit" disabled={loading}
                  className="w-full bg-noir text-ivory py-4 text-xs tracking-widest uppercase font-sans hover:bg-stone transition-colors duration-300 disabled:opacity-50">
                  {loading ? 'Sending Code...' : 'Continue'}
                </button>
              </div>
            </motion.form>
          ) : (
            <motion.form key="verify" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
              onSubmit={handleVerify} className="space-y-6">
              <p className="text-center text-xs tracking-widest font-sans text-stone">
                A verification code was sent to <span className="text-noir">{formData.email}</span>
              </p>
              <div>
                <label className={labelClass}>Verification Code</label>
                <input type="text" value={verificationCode} onChange={e => setVerificationCode(e.target.value)}
                  className={`${inputClass} text-center text-2xl tracking-widest-xl font-serif`}
                  placeholder="000000" maxLength={6} required />
              </div>

              {error && <p className="text-xs tracking-widest font-sans text-red-400 text-center">{error}</p>}

              <div className="pt-4 space-y-3">
                <button type="submit" disabled={loading}
                  className="w-full bg-noir text-ivory py-4 text-xs tracking-widest uppercase font-sans hover:bg-stone transition-colors duration-300 disabled:opacity-50">
                  {loading ? 'Creating Account...' : 'Verify & Register'}
                </button>
                <button type="button" onClick={() => setStep('form')}
                  className="w-full py-3 text-xs tracking-widest uppercase font-sans text-stone hover:text-noir transition-colors duration-300">
                  Back
                </button>
              </div>
            </motion.form>
          )}
        </AnimatePresence>

        <p className="text-center mt-8 text-xs tracking-widest font-sans text-stone">
          Already a member?{' '}
          <Link href="/auth/login" className="text-noir hover:text-gold transition-colors duration-300 underline underline-offset-4">
            Sign in
          </Link>
        </p>
      </motion.div>
    </div>
  )
}
