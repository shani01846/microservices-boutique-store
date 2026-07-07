'use client'

import Link from 'next/link'
import { useAuth } from '../lib/auth'

export default function Header() {
  const { user, logout, isAdmin } = useAuth()

  return (
    <header className="bg-blue-600 text-white shadow-lg">
      <div className="container mx-auto px-4 py-4">
        <div className="flex justify-between items-center">
          <Link href="/" className="text-2xl font-bold">
            VÈRA
          </Link>
          
          <nav className="flex items-center space-x-4">
            <Link href="/" className="hover:text-blue-200">
              Products
            </Link>
            
            {user ? (
              <>
                <Link href="/cart" className="hover:text-blue-200">
                  Cart
                </Link>
                
                <Link href="/orders" className="hover:text-blue-200">
                  My Orders
                </Link>
                
                {isAdmin() && (
                  <Link href="/admin" className="hover:text-blue-200">
                    Admin Panel
                  </Link>
                )}
                
                <span className="text-blue-200">
                  Hello, {user.firstName}
                </span>
                
                <button
                  onClick={logout}
                  className="bg-blue-500 hover:bg-blue-400 px-3 py-1 rounded"
                >
                  Logout
                </button>
              </>
            ) : (
              <div className="space-x-2">
                <Link
                  href="/auth/login"
                  className="bg-blue-500 hover:bg-blue-400 px-3 py-1 rounded"
                >
                  Login
                </Link>
                <Link
                  href="/auth/register"
                  className="bg-green-500 hover:bg-green-400 px-3 py-1 rounded"
                >
                  Register
                </Link>
              </div>
            )}
          </nav>
        </div>
      </div>
    </header>
  )
}