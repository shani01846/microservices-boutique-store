'use client'

import { useEffect, useState, useRef } from 'react'
import { useAuth } from '../../lib/auth'
import { apiRequest, API_BASE_URL } from '../../lib/api'
import { useRouter } from 'next/navigation'
import { motion, AnimatePresence } from 'framer-motion'
import { Plus, Pencil, Trash2, X } from 'lucide-react'

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

export default function AdminPanel() {
  const [products, setProducts] = useState<Product[]>([])
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [editingProduct, setEditingProduct] = useState<Product | null>(null)
  const [formData, setFormData] = useState({ name: '', description: '', price: 0, category: '', size: '', stockQuantity: 0 })
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [imagePreview, setImagePreview] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const { user, isAdmin } = useAuth()
  const router = useRouter()

  useEffect(() => {
    if (!user || !isAdmin()) { router.push('/'); return }
    fetchProducts()
  }, [user, router])

  const fetchProducts = async () => {
    try {
      setProducts(await apiRequest('/api/products'))
    } catch (error) { console.error(error) }
    finally { setLoading(false) }
  }

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    setImageFile(file)
    setImagePreview(URL.createObjectURL(file))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      let productId: string
      if (editingProduct) {
        await apiRequest(`/api/products/${editingProduct.id}`, { method: 'PUT', body: JSON.stringify(formData) })
        productId = editingProduct.id
      } else {
        const created = await apiRequest('/api/products', { method: 'POST', body: JSON.stringify(formData) })
        productId = created.id
      }
      if (imageFile) {
        const form = new FormData()
        form.append('file', imageFile)
        await apiRequest(`/api/products/${productId}/image`, { method: 'POST', body: form })
      }
      setShowForm(false); setEditingProduct(null); resetForm(); fetchProducts()
    } catch (error) { console.error(error) }
  }

  const deleteProduct = async (id: string) => {
    if (!confirm('Remove this piece from the collection?')) return
    try { await apiRequest(`/api/products/${id}`, { method: 'DELETE' }); fetchProducts() }
    catch (error) { console.error(error) }
  }

  const editProduct = (product: Product) => {
    setEditingProduct(product)
    setFormData({ name: product.name, description: product.description, price: product.price, category: product.category, size: product.size, stockQuantity: product.stockQuantity })
    setImagePreview(product.imageUrl ? `${API_BASE_URL}${product.imageUrl}` : null)
    setImageFile(null); setShowForm(true)
  }

  const resetForm = () => {
    setFormData({ name: '', description: '', price: 0, category: '', size: '', stockQuantity: 0 })
    setImageFile(null); setImagePreview(null)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target
    setFormData(prev => ({ ...prev, [name]: name === 'price' || name === 'stockQuantity' ? Number(value) : value }))
  }

  if (!user || !isAdmin()) return null
  if (loading) return (
    <div className="min-h-[60vh] flex items-center justify-center">
      <motion.div animate={{ opacity: [0.3, 1, 0.3] }} transition={{ duration: 2, repeat: Infinity }}
        className="font-serif text-2xl tracking-widest text-stone">Loading...</motion.div>
    </div>
  )

  const inputClass = "w-full px-0 py-2 border-0 border-b border-stone/30 bg-transparent font-sans text-sm text-noir focus:border-gold transition-colors duration-300"
  const labelClass = "block text-xs tracking-widest uppercase font-sans text-stone mb-2"

  return (
    <div>
      <motion.div initial={{ opacity: 0, y: 20 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.6 }}>
        <div className="flex justify-between items-end mb-12">
          <div>
            <p className="text-xs tracking-widest-xl uppercase font-sans text-gold mb-3">Management</p>
            <h1 className="font-serif text-5xl font-light text-noir">Atelier</h1>
          </div>
          <button
            onClick={() => { setShowForm(true); setEditingProduct(null); resetForm() }}
            className="flex items-center gap-2 border border-noir text-noir px-6 py-3 text-xs tracking-widest uppercase font-sans hover:bg-noir hover:text-ivory transition-all duration-300"
          >
            <Plus size={14} />
            Add Piece
          </button>
        </div>
        <div className="divider-gold mb-12" />

        {/* Product list */}
        <div className="space-y-px bg-stone/10">
          {products.map((product, i) => (
            <motion.div
              key={product.id}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              transition={{ delay: i * 0.04 }}
              className="bg-warm-white flex items-center gap-6 p-6"
            >
              <div className="w-16 h-20 bg-ivory flex-shrink-0 overflow-hidden">
                {product.imageUrl ? (
                  <img src={`${API_BASE_URL}${product.imageUrl}`} alt={product.name}
                    className="w-full h-full object-cover" />
                ) : (
                  <div className="w-full h-full flex items-center justify-center">
                    <span className="font-serif text-xl text-stone/40">{product.name[0]}</span>
                  </div>
                )}
              </div>
              <div className="flex-1 min-w-0">
                <h3 className="font-serif text-lg font-light text-noir mb-1">{product.name}</h3>
                <p className="text-stone text-xs font-sans truncate mb-2">{product.description}</p>
                <div className="flex gap-6 text-xs font-sans text-stone">
                  <span className="text-gold">{product.category}</span>
                  <span>Size: {product.size}</span>
                  <span>Stock: {product.stockQuantity}</span>
                </div>
              </div>
              <span className="font-serif text-xl text-noir flex-shrink-0">${product.price.toFixed(2)}</span>
              <div className="flex gap-3 flex-shrink-0">
                <button onClick={() => editProduct(product)}
                  className="w-9 h-9 border border-stone/30 flex items-center justify-center hover:border-noir transition-colors">
                  <Pencil size={13} className="text-stone" />
                </button>
                <button onClick={() => deleteProduct(product.id)}
                  className="w-9 h-9 border border-stone/30 flex items-center justify-center hover:border-red-400 transition-colors">
                  <Trash2 size={13} className="text-stone hover:text-red-400" />
                </button>
              </div>
            </motion.div>
          ))}
        </div>
      </motion.div>

      {/* Modal */}
      <AnimatePresence>
        {showForm && (
          <motion.div
            initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
            className="fixed inset-0 bg-noir/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
          >
            <motion.div
              initial={{ opacity: 0, y: 30, scale: 0.97 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: 30, scale: 0.97 }}
              transition={{ duration: 0.4, ease: [0.22, 1, 0.36, 1] }}
              className="bg-warm-white w-full max-w-lg max-h-[90vh] overflow-y-auto"
            >
              <div className="flex justify-between items-center p-8 pb-0">
                <div>
                  <p className="text-xs tracking-widest uppercase font-sans text-gold mb-1">
                    {editingProduct ? 'Edit' : 'New'}
                  </p>
                  <h2 className="font-serif text-2xl font-light text-noir">
                    {editingProduct ? editingProduct.name : 'Add Piece'}
                  </h2>
                </div>
                <button onClick={() => setShowForm(false)} className="text-stone hover:text-noir transition-colors">
                  <X size={18} />
                </button>
              </div>
              <div className="divider-gold mx-8 mt-4 mb-6" />

              <form onSubmit={handleSubmit} className="px-8 pb-8 space-y-5">
                <div>
                  <label className={labelClass}>Name</label>
                  <input type="text" name="name" value={formData.name} onChange={handleChange} className={inputClass} required />
                </div>
                <div>
                  <label className={labelClass}>Description</label>
                  <textarea name="description" value={formData.description} onChange={handleChange}
                    className={`${inputClass} resize-none`} rows={2} required />
                </div>
                <div className="grid grid-cols-2 gap-6">
                  <div>
                    <label className={labelClass}>Price</label>
                    <input type="number" step="0.01" name="price" value={formData.price} onChange={handleChange} className={inputClass} required />
                  </div>
                  <div>
                    <label className={labelClass}>Stock</label>
                    <input type="number" name="stockQuantity" value={formData.stockQuantity} onChange={handleChange} className={inputClass} required />
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-6">
                  <div>
                    <label className={labelClass}>Category</label>
                    <input type="text" name="category" value={formData.category} onChange={handleChange} className={inputClass} required />
                  </div>
                  <div>
                    <label className={labelClass}>Size</label>
                    <input type="text" name="size" value={formData.size} onChange={handleChange} className={inputClass} required />
                  </div>
                </div>
                <div>
                  <label className={labelClass}>Image</label>
                  {imagePreview && (
                    <img src={imagePreview} alt="Preview" className="w-full h-36 object-cover mb-3" />
                  )}
                  <input ref={fileInputRef} type="file" accept="image/jpeg,image/png,image/webp" onChange={handleImageChange}
                    className="w-full text-xs font-sans text-stone file:mr-4 file:py-2 file:px-4 file:border file:border-stone/30 file:bg-transparent file:text-xs file:tracking-widest file:uppercase file:font-sans file:text-stone hover:file:border-noir hover:file:text-noir file:transition-colors" />
                </div>
                <div className="flex gap-3 pt-4">
                  <button type="submit"
                    className="flex-1 bg-noir text-ivory py-3 text-xs tracking-widest uppercase font-sans hover:bg-stone transition-colors duration-300">
                    {editingProduct ? 'Update' : 'Create'}
                  </button>
                  <button type="button" onClick={() => setShowForm(false)}
                    className="flex-1 border border-stone/30 text-stone py-3 text-xs tracking-widest uppercase font-sans hover:border-noir hover:text-noir transition-colors duration-300">
                    Cancel
                  </button>
                </div>
              </form>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
