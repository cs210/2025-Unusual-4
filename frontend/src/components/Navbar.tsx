import { Glasses } from "lucide-react"
import Link from "next/link"

export default function Navbar() {
  return (
    <nav className="flex items-center justify-between px-6 py-4 backdrop-blur-sm border-b border-white/10">
      <Link href="/" className="flex items-center space-x-2">
        <Glasses className="w-8 h-8 text-purple-500" />
        <span className="text-white font-medium text-xl">XeleR</span>
      </Link>
    </nav>
  )
}

