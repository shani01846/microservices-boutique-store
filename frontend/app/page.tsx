'use client'

import { useEffect, useState } from 'react'
import { useAuth } from '../lib/auth'
import { apiRequest } from '../lib/api'

interface Product {
  id: number
  name: string
  description: string
  price: number
  category: string
  size: string
  stockQuantity: number
}

export default function Home() {
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const { user, token } = useAuth()

  useEffect(() => {
    fetchProducts()
  }, [])

  const fetchProducts = async () => {
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/products`)
      if (response.ok) {
        const data = await response.json()
        setProducts(data)
      }
    } catch (error) {
      console.error('Error fetching products:', error)
    } finally {
      setLoading(false)
    }
  }

  const addToCart = async (productId: number) => {
    if (!user) {
      alert('Please login to add items to cart')
      return
    }

    try {
      await apiRequest('/api/cart/add', {
        method: 'POST',
        body: JSON.stringify({ productId, quantity: 1 })
      })
      alert('Product added to cart!')
    } catch (error) {
      console.error('Error adding to cart:', error)
      alert('Error adding product to cart')
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-xl">Loading products...</div>
      </div>
    )
  }

  return (
    <div>
      <h1 className="text-4xl font-bold text-center mb-8 text-gray-800">
        Clothing Store
      </h1>
      
      {products.length === 0 ? (
        <div className="text-center">
          <p className="text-gray-600">No products available</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {products.map((product) => (
            <div
              key={product.id}
              className="bg-white rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow"
            >
              <h3 className="text-lg font-semibold mb-2 text-gray-800">
                {product.name}
              </h3>
              <p className="text-gray-600 mb-3 text-sm">
                {product.description}
              </p>
              <div className="space-y-2">
                <div className="flex justify-between">
                  <span className="text-sm text-gray-500">Category:</span>
                  <span className="text-sm font-medium">{product.category}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-500">Size:</span>
                  <span className="text-sm font-medium">{product.size}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-500">Stock:</span>
                  <span className={`text-sm font-medium ${
                    product.stockQuantity > 0 ? 'text-green-600' : 'text-red-600'
                  }`}>
                    {product.stockQuantity} units
                  </span>
                </div>
                <div className="flex justify-between items-center pt-2 border-t">
                  <span className="text-xl font-bold text-blue-600">
                    ${product.price.toFixed(2)}
                  </span>
                  <button
                    onClick={() => addToCart(product.id)}
                    className={`px-4 py-2 rounded text-sm font-medium ${
                      product.stockQuantity > 0
                        ? 'bg-blue-600 text-white hover:bg-blue-700'
                        : 'bg-gray-300 text-gray-500 cursor-not-allowed'
                    }`}
                    disabled={product.stockQuantity === 0}
                  >
                    {product.stockQuantity > 0 ? 'Add to Cart' : 'Out of Stock'}
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}