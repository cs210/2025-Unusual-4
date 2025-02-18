"use client"

import { useEffect, useState } from "react"
import { useRouter } from "next/navigation"
import { supabase } from "@/utils/supabase"
import type { Chat } from "@/utils/supabase"
import { motion, AnimatePresence } from "framer-motion"

const ChatHistory = () => {
  const router = useRouter()
  const [chats, setChats] = useState<Chat[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const loadChatHistory = async () => {
      setLoading(true)
      // Get chat IDs from localStorage
      const chatIds = JSON.parse(localStorage.getItem("chatHistory") || "[]")

      if (chatIds.length === 0) {
        setLoading(false)
        return
      }

      try {
        // Fetch chat details from Supabase
        const { data, error } = await supabase
          .from("chats")
          .select("*")
          .in("id", chatIds)
          .order("created_at", { ascending: false })

        if (error) {
          console.error("Error fetching chat history:", error)
          return
        }

        if (data) {
          // Keep only the latest 3 chats
          setChats(data.slice(0, 3))
        }
      } catch (error) {
        console.error("Error in loadChatHistory:", error)
      } finally {
        setLoading(false)
      }
    }

    loadChatHistory()
  }, [])

  const handleChatClick = (chat: Chat) => {
    router.push(`/chat?id=${chat.id}`)
  }

  if (loading) {
    return (
      <div className="bg-gradient-to-br from-purple-900/30 to-pink-900/30 rounded-2xl p-6 backdrop-blur-sm border border-purple-500/20">
        <h2 className="text-2xl font-semibold mb-6 text-transparent bg-clip-text bg-gradient-to-r from-purple-400 to-pink-600">
          Recent Chats
        </h2>
        <p className="text-white">Loading chat history...</p>
      </div>
    )
  }

  if (chats.length === 0) {
    return (
      <div className="bg-gradient-to-br from-purple-900/30 to-pink-900/30 rounded-2xl p-6 backdrop-blur-sm border border-purple-500/20">
        <h2 className="text-2xl font-semibold mb-6 text-transparent bg-clip-text bg-gradient-to-r from-purple-400 to-pink-600">
          Recent Chats
        </h2>
        <p className="text-white">No recent chats found. Start a new conversation!</p>
      </div>
    )
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.5 }}
      className="bg-gradient-to-br from-purple-900/30 to-pink-900/30 rounded-2xl p-6 backdrop-blur-sm border border-purple-500/20"
    >
      <h2 className="text-2xl font-semibold mb-6 text-transparent bg-clip-text bg-gradient-to-r from-purple-400 to-pink-600">
        Recent Chats
      </h2>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <AnimatePresence>
          {chats.map((chat) => (
            <motion.button
              key={chat.id}
              initial={{ opacity: 0, scale: 0.95 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.95 }}
              whileHover={{ scale: 1.02 }}
              whileTap={{ scale: 0.98 }}
              onClick={() => handleChatClick(chat)}
              className="p-4 rounded-xl border border-purple-500/20 hover:border-pink-500/50 transition-colors duration-200 text-left bg-gradient-to-r from-purple-800/50 to-pink-800/50 text-white group"
              tabIndex={0}
              aria-label={`Continue chat: ${chat.title}`}
            >
              <h3 className="font-medium truncate group-hover:text-pink-400 transition-colors">{chat.title}</h3>
              <p className="text-sm text-slate-400">{new Date(chat.created_at).toLocaleDateString()}</p>
            </motion.button>
          ))}
        </AnimatePresence>
      </div>
    </motion.div>
  )
}

export default ChatHistory

