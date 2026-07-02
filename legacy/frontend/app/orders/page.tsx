'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { useAuth } from '../../lib/auth'
import { apiRequest } from '../../lib/api'

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
    apiRequest('/api/orders')
      .then(setOrders)
      .finally(() => setLoading(false))
  }, [user])

  if (isLoading || loading) return <div className="text-center py-10">Loading...</div>

  return (
    <div className="max-w-3xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">My Orders</h1>

      {orders.length === 0 ? (
        <p className="text-gray-500">No orders yet.</p>
      ) : (
        <div className="space-y-4">
          {orders.map(order => (
            <div key={order.id} className="bg-white rounded-lg shadow-md overflow-hidden">
              <button
                className="w-full flex justify-between items-center px-6 py-4 hover:bg-gray-50 transition"
                onClick={() => setExpandedId(expandedId === order.id ? null : order.id)}
              >
                <div className="flex items-center gap-4">
                  <span className="font-medium">Order #{order.id}</span>
                  <span className={`text-xs px-2 py-1 rounded-full font-medium ${
                    order.status === 'Confirmed' ? 'bg-green-100 text-green-700' :
                    order.status === 'Pending'   ? 'bg-yellow-100 text-yellow-700' :
                                                   'bg-gray-100 text-gray-600'
                  }`}>
                    {order.status}
                  </span>
                </div>
                <div className="flex items-center gap-6 text-sm text-gray-600">
                  <span>{new Date(order.orderDate).toLocaleDateString()}</span>
                  <span className="font-semibold text-gray-900">${order.totalAmount.toFixed(2)}</span>
                  <span>{expandedId === order.id ? '▲' : '▼'}</span>
                </div>
              </button>

              {expandedId === order.id && (
                <div className="border-t px-6 py-4 bg-gray-50">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="text-gray-500 text-left">
                        <th className="pb-2">Product</th>
                        <th className="pb-2 text-center">Qty</th>
                        <th className="pb-2 text-right">Unit Price</th>
                        <th className="pb-2 text-right">Subtotal</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-200">
                      {order.items.map(item => (
                        <tr key={item.id}>
                          <td className="py-2">{item.productName}</td>
                          <td className="py-2 text-center">{item.quantity}</td>
                          <td className="py-2 text-right">${item.unitPrice.toFixed(2)}</td>
                          <td className="py-2 text-right">${(item.unitPrice * item.quantity).toFixed(2)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
