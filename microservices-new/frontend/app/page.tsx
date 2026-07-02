'use client'

import { useEffect, useState } from 'react'
import { useAuth } from '../lib/auth'
import { apiRequest } from '../lib/api'
import { motion, AnimatePresence } from 'framer-motion'
import { ShoppingBag, Plus } from 'lucide-react'

interface Product {
  id: string
  name: string
  description: string
  price: number
  category: string
  size: string
  stockQuantity: number
  imageUrl?: string
}

const fadeUp = {
  hidden: { opacity: 0, y: 30 },
  visible: (i: number) => ({
    opacity: 1, y: 0,
    transition: { delay: i * 0.08, duration: 0.6, ease: [0.22, 1, 0.36, 1] }
  })
}

export default function Home() {
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [notification, setNotification] = useState<string | null>(null)
  const { user } = useAuth()

  useEffect(() => { fetchProducts() }, [])

  const fetchProducts = async () => {
    try {
      const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/products`)
      if (response.ok) setProducts(await response.json())
    } catch (error) {
      console.error('Error fetching products:', error)
    } finally {
      setLoading(false)
    }
  }

  const addToCart = async (product: Product) => {
    if (!user) {
      setNotification('Please sign in to add items')
      setTimeout(() => setNotification(null), 3000)
      return
    }
    try {
      await apiRequest('/api/cart/add', {
        method: 'POST',
        body: JSON.stringify({ productId: product.id, productName: product.name, price: product.price, quantity: 1 })
      })
      setNotification(`${product.name} added to your bag`)
      setTimeout(() => setNotification(null), 3000)
    } catch {
      setNotification('Unable to add item')
      setTimeout(() => setNotification(null), 3000)
    }
  }

  if (loading) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <motion.div
          animate={{ opacity: [0.3, 1, 0.3] }}
          transition={{ duration: 2, repeat: Infinity }}
          className="font-serif text-2xl tracking-widest text-stone"
        >
          MAISON
        </motion.div>
      </div>
    )
  }

  return (
    <div>
      {/* Notification toast */}
      <AnimatePresence>
        {notification && (
          <motion.div
            initial={{ opacity: 0, y: -20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -20 }}
            className="fixed top-24 left-1/2 -translate-x-1/2 z-50 bg-noir text-ivory px-8 py-3 text-xs tracking-widest uppercase font-sans"
          >
            {notification}
          </motion.div>
        )}
      </AnimatePresence>

      {/* Hero section */}
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ duration: 1 }}
        className="text-center mb-16 pt-8"
      >
        <p className="text-xs tracking-widest-xl uppercase font-sans text-gold mb-4">New Collection</p>
        <h2 className="font-serif text-5xl md:text-7xl font-light text-noir mb-6 leading-none">
          The Edit
        </h2>
        <div className="divider-gold w-32 mx-auto mb-6" />
        <p className="text-stone text-sm tracking-widest font-sans font-light max-w-md mx-auto">
          Curated pieces for the discerning wardrobe
        </p>
      </motion.div>

      {products.length === 0 ? (
        <div className="text-center py-24">
          <p className="font-serif text-2xl text-stone italic">No pieces available at this time</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-px bg-stone/10">
          {products.map((product, i) => (
            <motion.div
              key={product.id}
              custom={i}
              variants={fadeUp}
              initial="hidden"
              animate="visible"
              className="bg-warm-white group relative overflow-hidden"
            >
              {/* Image */}
              <div className="relative overflow-hidden aspect-[3/4] bg-ivory">
                {product.imageUrl ? (
                  <motion.img
                    src={`${process.env.NEXT_PUBLIC_API_URL}${product.imageUrl}`}
                    alt={product.name}
                    className="w-full h-full object-cover transition-transform duration-700 group-hover:scale-105"
                  />
                ) : (
                  <div className="w-full h-full flex flex-col items-center justify-center text-stone/40">
                    <div className="w-12 h-12 border border-stone/20 flex items-center justify-center mb-3">
                      <span className="font-serif text-lg">{product.name[0]}</span>
                    </div>
                    <span className="text-xs tracking-widest uppercase font-sans">No Image</span>
                  </div>
                )}

                {/* Overlay on hover */}
                <motion.div
                  initial={{ opacity: 0 }}
                  whileHover={{ opacity: 1 }}
                  className="absolute inset-0 bg-noir/20 flex items-end justify-center pb-6"
                >
                  <button
                    onClick={() => addToCart(product)}
                    disabled={product.stockQuantity === 0}
                    className={`flex items-center gap-2 px-6 py-3 text-xs tracking-widest uppercase font-sans transition-all duration-300 ${
                      product.stockQuantity > 0
                        ? 'bg-warm-white text-noir hover:bg-noir hover:text-ivory'
                        : 'bg-stone/50 text-ivory cursor-not-allowed'
                    }`}
                  >
                    <ShoppingBag size={14} />
                    {product.stockQuantity > 0 ? 'Add to Bag' : 'Sold Out'}
                  </button>
                </motion.div>

                {product.stockQuantity === 0 && (
                  <div className="absolute top-4 left-4 bg-noir text-ivory text-xs tracking-widest uppercase font-sans px-3 py-1">
                    Sold Out
                  </div>
                )}
              </div>

              {/* Product info */}
              <div className="p-5">
                <div className="flex justify-between items-start mb-1">
                  <p className="text-xs tracking-widest uppercase font-sans text-gold">{product.category}</p>
                  <p className="text-xs tracking-widest font-sans text-stone">{product.size}</p>
                </div>
                <h3 className="font-serif text-lg font-light text-noir mb-1">{product.name}</h3>
                <p className="text-stone text-xs font-sans mb-3 line-clamp-2 leading-relaxed">{product.description}</p>
                <div className="flex justify-between items-center pt-3 border-t border-stone/10">
                  <span className="font-serif text-xl text-noir">${product.price.toFixed(2)}</span>
                  <button
                    onClick={() => addToCart(product)}
                    disabled={product.stockQuantity === 0}
                    className={`md:hidden flex items-center gap-1 text-xs tracking-widest uppercase font-sans transition-colors duration-300 ${
                      product.stockQuantity > 0 ? 'text-noir hover:text-gold' : 'text-stone/40 cursor-not-allowed'
                    }`}
                  >
                    <Plus size={12} />
                    Bag
                  </button>
                </div>
              </div>
            </motion.div>
          ))}
        </div>
      )}
    </div>
  )
}
