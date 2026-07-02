'use client'

import { useEffect, useState } from 'react'
import { useAuth } from '../../lib/auth'
import { apiRequest } from '../../lib/api'
import { useRouter } from 'next/navigation'

interface CartItem {
  id: number
  productId: number
  productName: string
  price: number
  quantity: number
  subtotal: number
}

interface Cart {
  id: number
  userId: number
  items: CartItem[]
  total: number
}

export default function CartPage() {
  const [cart, setCart] = useState<Cart | null>(null)
  const [loading, setLoading] = useState(true)
  const { user, token } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!user) {
      router.push('/auth/login')
      return
    }
    fetchCart()
  }, [user, router])

  const fetchCart = async () => {
    try {
      const data = await apiRequest('/api/cart')
      setCart(data)
    } catch (error) {
      console.error('Error fetching cart:', error)
    } finally {
      setLoading(false)
    }
  }

  const updateQuantity = async (itemId: number, quantity: number) => {
    try {
      await apiRequest(`/api/cart/update/${itemId}`, {
        method: 'PUT',
        body: JSON.stringify(quantity)
      })
      fetchCart()
    } catch (error) {
      console.error('Error updating quantity:', error)
    }
  }

  const removeItem = async (itemId: number) => {
    try {
      await apiRequest(`/api/cart/remove/${itemId}`, {
        method: 'DELETE'
      })
      fetchCart()
    } catch (error) {
      console.error('Error removing item:', error)
    }
  }

  const checkout = async () => {
    try {
      await apiRequest('/api/orders/from-cart', {
        method: 'POST'
      })
      alert('Order placed successfully!')
      fetchCart()
    } catch (error) {
      console.error('Error placing order:', error)
      alert('Error placing order')
    }
  }

  if (!user) {
    return <div>Please login to view your cart</div>
  }

  if (loading) {
    return <div>Loading cart...</div>
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="text-center">
        <h1 className="text-3xl font-bold mb-4">Your Cart</h1>
        <p className="text-gray-600">Your cart is empty</p>
      </div>
    )
  }

  return (
    <div className="max-w-4xl mx-auto">
      <h1 className="text-3xl font-bold mb-6">Your Cart</h1>
      
      <div className="bg-white rounded-lg shadow-md p-6">
        {cart.items.map((item) => (
          <div key={item.id} className="flex items-center justify-between border-b py-4 last:border-b-0">
            <div className="flex-1">
              <h3 className="font-semibold">{item.productName}</h3>
              <p className="text-gray-600">${item.price.toFixed(2)} each</p>
            </div>
            
            <div className="flex items-center space-x-4">
              <div className="flex items-center space-x-2">
                <button
                  onClick={() => updateQuantity(item.id, item.quantity - 1)}
                  className="bg-gray-200 hover:bg-gray-300 px-2 py-1 rounded"
                >
                  -
                </button>
                <span className="px-3 py-1 border rounded">{item.quantity}</span>
                <button
                  onClick={() => updateQuantity(item.id, item.quantity + 1)}
                  className="bg-gray-200 hover:bg-gray-300 px-2 py-1 rounded"
                >
                  +
                </button>
              </div>
              
              <div className="text-lg font-semibold">
                ${item.subtotal.toFixed(2)}
              </div>
              
              <button
                onClick={() => removeItem(item.id)}
                className="text-red-600 hover:text-red-800"
              >
                Remove
              </button>
            </div>
          </div>
        ))}
        
        <div className="flex justify-between items-center pt-4 mt-4 border-t">
          <div className="text-xl font-bold">
            Total: ${cart.total.toFixed(2)}
          </div>
          
          <button
            onClick={checkout}
            className="bg-green-600 text-white px-6 py-2 rounded-lg hover:bg-green-700"
          >
            Checkout
          </button>
        </div>
      </div>
    </div>
  )
}