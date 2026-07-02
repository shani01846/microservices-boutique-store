'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { useAuth } from '../../lib/auth'
import { apiRequest } from '../../lib/api'
import { motion, AnimatePresence } from 'framer-motion'
import { ChevronDown } from 'lucide-react'

interface OrderItem {
  id: number
  productId: number
  productName: string
  quantity: number
  unitPrice: number
}

interface Order {
  id: number
  orderDate: string
  totalAmount: number
  status: string
  items: OrderItem[]
}

const statusStyle: Record<string, string> = {
  Confirmed: 'text-gold border-gold',
  Pending: 'text-stone border-stone',
}

export default function OrdersPage() {
  const { user, isLoading } = useAuth()
  const router = useRouter()
  const [orders, setOrders] = useState<Order[]>([])
  const [loading, setLoading] = useState(true)
  const [expandedId, setExpandedId] = useState<number | null>(null)

  useEffect(() => {
    if (!isLoading && !user) router.push('/auth/login')
  }, [user, isLoading, router])

  useEffect(() => {
    if (!user) return
    apiRequest('/api/orders').then(setOrders).finally(() => setLoading(false))
  }, [user])

  if (isLoading || loading) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <motion.div animate={{ opacity: [0.3, 1, 0.3] }} transition={{ duration: 2, repeat: Infinity }}
          className="font-serif text-2xl tracking-widest text-stone">Loading...</motion.div>
      </div>
    )
  }

  return (
    <div className="max-w-3xl mx-auto">
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.6 }}>
        <div className="text-center mb-12">
          <p className="text-xs tracking-widest-xl uppercase font-sans text-gold mb-3">History</p>
          <h1 className="font-serif text-5xl font-light text-noir mb-4">My Orders</h1>
          <div className="divider-gold w-24 mx-auto" />
        </div>

        {orders.length === 0 ? (
          <div className="text-center py-24">
            <p className="font-serif text-2xl text-stone italic">No orders yet</p>
          </div>
        ) : (
          <div className="space-y-px bg-stone/10">
            {orders.map((order, i) => (
              <motion.div
                key={order.id}
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ delay: i * 0.06 }}
                className="bg-warm-white"
              >
                <button
                  className="w-full flex justify-between items-center px-8 py-6 hover:bg-ivory transition-colors duration-300"
                  onClick={() => setExpandedId(expandedId === order.id ? null : order.id)}
                >
                  <div className="flex items-center gap-6">
                    <span className="font-serif text-lg font-light text-noir">Order #{order.id}</span>
                    <span className={`text-xs tracking-widest uppercase font-sans border px-3 py-1 ${statusStyle[order.status] ?? 'text-stone border-stone/30'}`}>
                      {order.status}
                    </span>
                  </div>
                  <div className="flex items-center gap-8">
                    <span className="text-xs tracking-widest font-sans text-stone">
                      {new Date(order.orderDate).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}
                    </span>
                    <span className="font-serif text-xl">${order.totalAmount.toFixed(2)}</span>
                    <motion.div animate={{ rotate: expandedId === order.id ? 180 : 0 }} transition={{ duration: 0.3 }}>
                      <ChevronDown size={14} className="text-stone" />
                    </motion.div>
                  </div>
                </button>

                <AnimatePresence>
                  {expandedId === order.id && (
                    <motion.div
                      initial={{ height: 0, opacity: 0 }}
                      animate={{ height: 'auto', opacity: 1 }}
                      exit={{ height: 0, opacity: 0 }}
                      transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1] }}
                      className="overflow-hidden"
                    >
                      <div className="px-8 pb-6 bg-ivory">
                        <div className="divider-gold mb-6" />
                        <table className="w-full text-sm">
                          <thead>
                            <tr className="text-left">
                              <th className="pb-3 text-xs tracking-widest uppercase font-sans text-stone font-normal">Item</th>
                              <th className="pb-3 text-xs tracking-widest uppercase font-sans text-stone font-normal text-center">Qty</th>
                              <th className="pb-3 text-xs tracking-widest uppercase font-sans text-stone font-normal text-right">Price</th>
                              <th className="pb-3 text-xs tracking-widest uppercase font-sans text-stone font-normal text-right">Total</th>
                            </tr>
                          </thead>
                          <tbody>
                            {order.items.map(item => (
                              <tr key={item.id} className="border-t border-stone/10">
                                <td className="py-3 font-serif text-base font-light">{item.productName}</td>
                                <td className="py-3 text-center font-sans text-stone text-xs">{item.quantity}</td>
                                <td className="py-3 text-right font-serif">${item.unitPrice.toFixed(2)}</td>
                                <td className="py-3 text-right font-serif">${(item.unitPrice * item.quantity).toFixed(2)}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    </motion.div>
                  )}
                </AnimatePresence>
              </motion.div>
            ))}
          </div>
        )}
      </motion.div>
    </div>
  )
}
