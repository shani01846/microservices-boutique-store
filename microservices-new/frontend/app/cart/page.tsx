'use client'

import { useEffect, useState } from 'react'
import { useAuth } from '../../lib/auth'
import { apiRequest } from '../../lib/api'
import { useRouter } from 'next/navigation'
import { motion, AnimatePresence } from 'framer-motion'
import { Minus, Plus, X, ShoppingBag } from 'lucide-react'

interface CartItem {
  productId: string
  productName: string
  price: number
  quantity: number
  subtotal: number
}

interface Cart {
  userId: string
  items: CartItem[]
  total: number
}

export default function CartPage() {
  const [cart, setCart] = useState<Cart | null>(null)
  const [loading, setLoading] = useState(true)
  const [checkingOut, setCheckingOut] = useState(false)
  const [success, setSuccess] = useState(false)
  const { user } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!user) { router.push('/auth/login'); return }
    fetchCart()
  }, [user, router])

  const fetchCart = async () => {
    try {
      setCart(await apiRequest('/api/cart'))
    } catch (error) {
      console.error('Error fetching cart:', error)
    } finally {
      setLoading(false)
    }
  }

  const updateQuantity = async (productId: string, quantity: number) => {
    try {
      await apiRequest(`/api/cart/update/${productId}`, { method: 'PUT', body: JSON.stringify(quantity) })
      fetchCart()
    } catch (error) { console.error(error) }
  }

  const removeItem = async (productId: string) => {
    try {
      await apiRequest(`/api/cart/remove/${productId}`, { method: 'DELETE' })
      fetchCart()
    } catch (error) { console.error(error) }
  }

  const checkout = async () => {
    setCheckingOut(true)
    try {
      await apiRequest('/api/orders/from-cart', { method: 'POST' })
      setSuccess(true)
      fetchCart()
    } catch (error) {
      console.error(error)
    } finally {
      setCheckingOut(false)
    }
  }

  if (!user) return null

  if (loading) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <motion.div animate={{ opacity: [0.3, 1, 0.3] }} transition={{ duration: 2, repeat: Infinity }}
          className="font-serif text-2xl tracking-widest text-stone">Loading...</motion.div>
      </div>
    )
  }

  return (
    <div className="max-w-4xl mx-auto">
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.6 }}>
        <div className="text-center mb-12">
          <p className="text-xs tracking-widest-xl uppercase font-sans text-gold mb-3">Your Selection</p>
          <h1 className="font-serif text-5xl font-light text-noir mb-4">Shopping Bag</h1>
          <div className="divider-gold w-24 mx-auto" />
        </div>

        <AnimatePresence>
          {success && (
            <motion.div
              initial={{ opacity: 0, y: -10 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }}
              className="bg-noir text-ivory text-center py-4 mb-8 text-xs tracking-widest uppercase font-sans"
            >
              Your order has been placed — thank you
            </motion.div>
          )}
        </AnimatePresence>

        {!cart || cart.items.length === 0 ? (
          <div className="text-center py-24">
            <ShoppingBag size={32} className="text-stone/30 mx-auto mb-6" />
            <p className="font-serif text-2xl text-stone italic mb-6">Your bag is empty</p>
            <button onClick={() => router.push('/')}
              className="text-xs tracking-widest uppercase font-sans border border-noir text-noir px-8 py-3 hover:bg-noir hover:text-ivory transition-all duration-300">
              Continue Shopping
            </button>
          </div>
        ) : (
          <div className="grid md:grid-cols-3 gap-12">
            {/* Items */}
            <div className="md:col-span-2 space-y-0">
              {cart.items.map((item, i) => (
                <motion.div
                  key={item.productId}
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: -20 }}
                  transition={{ delay: i * 0.05 }}
                  className="flex items-center gap-6 py-6 border-b border-stone/10"
                >
                  <div className="w-20 h-24 bg-ivory flex-shrink-0 flex items-center justify-center">
                    <span className="font-serif text-2xl text-stone/40">{item.productName[0]}</span>
                  </div>
                  <div className="flex-1">
                    <h3 className="font-serif text-lg font-light text-noir mb-1">{item.productName}</h3>
                    <p className="text-xs tracking-widest font-sans text-stone">${item.price.toFixed(2)}</p>
                  </div>
                  <div className="flex items-center gap-3">
                    <button onClick={() => updateQuantity(item.productId, item.quantity - 1)}
                      className="w-7 h-7 border border-stone/30 flex items-center justify-center hover:border-noir transition-colors">
                      <Minus size={10} />
                    </button>
                    <span className="font-sans text-sm w-4 text-center">{item.quantity}</span>
                    <button onClick={() => updateQuantity(item.productId, item.quantity + 1)}
                      className="w-7 h-7 border border-stone/30 flex items-center justify-center hover:border-noir transition-colors">
                      <Plus size={10} />
                    </button>
                  </div>
                  <span className="font-serif text-lg w-20 text-right">${item.subtotal.toFixed(2)}</span>
                  <button onClick={() => removeItem(item.productId)} className="text-stone/40 hover:text-noir transition-colors">
                    <X size={14} />
                  </button>
                </motion.div>
              ))}
            </div>

            {/* Summary */}
            <motion.div
              initial={{ opacity: 0, x: 20 }} animate={{ opacity: 1, x: 0 }} transition={{ delay: 0.3 }}
              className="bg-ivory p-8 h-fit"
            >
              <h2 className="font-serif text-xl font-light text-noir mb-6 tracking-wide">Order Summary</h2>
              <div className="divider-gold mb-6" />
              <div className="flex justify-between mb-3">
                <span className="text-xs tracking-widest uppercase font-sans text-stone">Subtotal</span>
                <span className="font-serif text-lg">${cart.total.toFixed(2)}</span>
              </div>
              <div className="flex justify-between mb-6">
                <span className="text-xs tracking-widest uppercase font-sans text-stone">Shipping</span>
                <span className="text-xs font-sans text-gold tracking-widest">Complimentary</span>
              </div>
              <div className="divider-gold mb-6" />
              <div className="flex justify-between mb-8">
                <span className="text-xs tracking-widest uppercase font-sans text-noir">Total</span>
                <span className="font-serif text-2xl">${cart.total.toFixed(2)}</span>
              </div>
              <button
                onClick={checkout}
                disabled={checkingOut}
                className="w-full bg-noir text-ivory py-4 text-xs tracking-widest uppercase font-sans hover:bg-stone transition-colors duration-300 disabled:opacity-50"
              >
                {checkingOut ? 'Processing...' : 'Place Order'}
              </button>
            </motion.div>
          </div>
        )}
      </motion.div>
    </div>
  )
}
