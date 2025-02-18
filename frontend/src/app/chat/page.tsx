"use client"

import { AnimatePresence, motion } from "framer-motion"
import { Suspense, useEffect, useRef, useState, useCallback } from "react"
import dynamic from "next/dynamic"

// Dynamically import components that use browser APIs
const CodeView = dynamic(() => import("@/components/CodeView"), { ssr: false })
const SceneView = dynamic(() => import("@/components/3DSceneView"), { ssr: false })

import type { Message } from "@/types/chat"
import { configureMarked } from "@/utils/markdown"
import { extractCodeBlocks } from "@/utils/code"
import { marked } from "marked"
import { supabase } from "@/utils/supabase"
import { useSearchParams } from "next/navigation"
import Navbar from "@/components/Navbar"

// Move localStorage operations to a utility function
const updateChatHistory = (chatId: string) => {
  try {
    if (typeof window === "undefined") return
    const chatHistory = JSON.parse(localStorage.getItem("chatHistory") || "[]")
    if (!chatHistory.includes(chatId)) {
      localStorage.setItem("chatHistory", JSON.stringify([...chatHistory, chatId]))
    }
  } catch (error) {
    console.warn("Error updating chat history:", error)
  }
}

const ChatPageContent = () => {
  const searchParams = useSearchParams()
  const initialQuestion = searchParams.get("q")
  const initialCode = searchParams.get("code")
  const chatId = searchParams.get("id")

  const [messages, setMessages] = useState<Message[]>([])
  const [input, setInput] = useState("")
  const [isLoading, setIsLoading] = useState(false)
  const [currentChatId, setCurrentChatId] = useState<string | null>(chatId)
  const [chatTitle, setChatTitle] = useState<string>("")
  const [initialQuestionProcessed, setInitialQuestionProcessed] = useState(false)
  const [streamingMessage, setStreamingMessage] = useState("")
  const [isInitialized, setIsInitialized] = useState(false)
  const [hasCode, setHasCode] = useState(false)
  const [activeTab, setActiveTab] = useState<"code" | "scene">("code")
  const [latestCodeBlock, setLatestCodeBlock] = useState<{ code: string; language: string } | null>(null)

  const activeRequestRef = useRef<boolean>(false)

  useEffect(() => {
    if (typeof window !== "undefined") {
      configureMarked()
    }
  }, [])

  useEffect(() => {
    console.log("ChatPageContent mounted")
    return () => {
      console.log("ChatPageContent unmounted")
    }
  }, [])

  const handleSendMessage = useCallback(
    async (content: string) => {
      if (!content.trim() || activeRequestRef.current) return

      try {
        activeRequestRef.current = true
        const newMessage: Message = { role: "user", content }
        setMessages((prev) => [...prev, newMessage])
        setInput("")
        setIsLoading(true)
        setStreamingMessage("")

        if (currentChatId) {
          await supabase.from("messages").insert([
            {
              chat_id: currentChatId,
              role: "user",
              content,
            },
          ])
        }

        console.log("Sending message to API...")
        const response = await fetch("/api/chat", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            message: content,
            messages: [...messages, newMessage],
          }),
        })

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`)
        }

        if (!response.body) {
          throw new Error("Stream response not available")
        }

        const reader = response.body.getReader()
        const decoder = new TextDecoder()
        let fullMessage = ""

        try {
          while (true) {
            const { done, value } = await reader.read()
            if (done) break

            const text = decoder.decode(value)
            fullMessage += text
            setStreamingMessage(fullMessage)
            console.log("Received chunk:", text)
          }

          if (currentChatId) {
            await supabase.from("messages").insert([
              {
                chat_id: currentChatId,
                role: "assistant",
                content: fullMessage,
              },
            ])
          }

          setMessages((prev) => [...prev, { role: "assistant", content: fullMessage }])
        } catch (error) {
          console.error("Error in streaming:", error)
          // Fallback: If streaming fails, try to get the full response
          const fullResponse = await response.text()
          console.log("Full response:", fullResponse)
          setMessages((prev) => [
            ...prev,
            { role: "assistant", content: fullResponse || "Sorry, there was an error processing your request." },
          ])
        } finally {
          setStreamingMessage("")
        }
      } catch (error) {
        console.error("Error:", error)
        setMessages((prev) => [
          ...prev,
          { role: "assistant", content: "Sorry, there was an error processing your request. Please try again." },
        ])
      } finally {
        setIsLoading(false)
        activeRequestRef.current = false
      }
    },
    [currentChatId, messages],
  )

  useEffect(() => {
    const initializeChat = async () => {
      if (!chatId && !initialQuestion) return
      if (isInitialized) return
      setIsInitialized(true)

      try {
        if (chatId) {
          const { data: chat } = await supabase.from("chats").select("*").eq("id", chatId).single()

          if (chat) {
            setCurrentChatId(chat.id)
            setChatTitle(chat.title)
            updateChatHistory(chat.id)

            const { data: chatMessages } = await supabase
              .from("messages")
              .select("*")
              .eq("chat_id", chat.id)
              .order("created_at", { ascending: true })

            if (chatMessages) {
              setMessages(
                chatMessages.map((msg) => ({
                  role: msg.role as "user" | "assistant",
                  content: msg.content,
                })),
              )
            }
          }
        }

        if (initialQuestion && !initialQuestionProcessed) {
          setInitialQuestionProcessed(true)
          const initialMessage: Message = { role: "user", content: initialQuestion }
          setMessages([initialMessage])
          handleSendMessage(initialQuestion)

          if (initialCode) {
            setLatestCodeBlock({ code: decodeURIComponent(initialCode), language: "javascript" })
            setHasCode(true)
            setActiveTab("scene")
          }
        }
      } catch (error) {
        console.error("Error initializing chat:", error)
      }
    }

    initializeChat()
  }, [chatId, initialQuestion, initialCode, isInitialized, initialQuestionProcessed, handleSendMessage])

  useEffect(() => {
    if (!chatId && !initialQuestion) {
      setIsInitialized(false)
      setInitialQuestionProcessed(false)
    }
  }, [chatId, initialQuestion])

  useEffect(() => {
    // Extract the latest code block from the assistant's responses
    const lastMessageWithCode = [...messages].reverse().find((message) => {
      if (message.role === "assistant") {
        const blocks = extractCodeBlocks(message.content)
        return blocks.some((block) => block.isCode)
      }
      return false
    })

    if (lastMessageWithCode) {
      const blocks = extractCodeBlocks(lastMessageWithCode.content)
      const latestBlock = blocks.find((block) => block.isCode)
      if (latestBlock) {
        // Wrap the code in a function to ensure it's executed in the animation loop
        const wrappedCode = `
          function initScene() {
            ${latestBlock.code}
          }
          initScene();
          
          function animate() {
            requestAnimationFrame(animate);
            if (controls) controls.update();
            renderer.render(scene, camera);
          }
          animate();
        `
        setLatestCodeBlock({ code: wrappedCode, language: latestBlock.language })
        setHasCode(true)
      } else {
        setHasCode(false)
      }
    } else {
      setHasCode(false)
      setLatestCodeBlock(null)
    }
  }, [messages])

  const renderMessage = (content: string) => {
    const blocks = extractCodeBlocks(content)

    return blocks.map((block, index) =>
      block.isCode ? (
        <CodeView key={index} code={block.code} language={block.language} />
      ) : (
        <div
          key={index}
          className="prose dark:prose-invert max-w-none"
          dangerouslySetInnerHTML={{ __html: marked(block.content) }}
        />
      ),
    )
  }

  return (
    <main className="min-h-screen bg-black antialiased relative overflow-hidden">
      <Navbar />

      <div className="container mx-auto px-6 relative z-20 pt-20 pb-16">
        <AnimatePresence>
          {chatTitle && (
            <motion.h1
              initial={{ opacity: 0, y: -20 }}
              animate={{ opacity: 1, y: 0 }}
              className="text-3xl font-bold mb-6 text-transparent bg-clip-text bg-gradient-to-r from-purple-400 to-pink-600"
            >
              {chatTitle}
            </motion.h1>
          )}
        </AnimatePresence>

        <div className={`${hasCode ? "grid grid-cols-2 gap-4" : "flex justify-center"} max-w-full`}>
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className={`space-y-4 mb-24 ${hasCode ? "px-4" : "w-[60%] px-8 py-6 bg-gradient-to-br from-purple-900/30 to-pink-900/30 rounded-2xl backdrop-blur-sm border border-purple-500/20"}`}
          >
            <AnimatePresence>
              {messages.map((message, index) => (
                <motion.div
                  key={index}
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -20 }}
                  transition={{ duration: 0.2 }}
                  className={`p-4 rounded-xl max-w-[90%] ${
                    message.role === "user"
                      ? "bg-gradient-to-r from-purple-600 to-pink-600 text-white ml-auto"
                      : "bg-gradient-to-r from-gray-800 to-gray-700 text-white mr-auto"
                  }`}
                >
                  {message.role === "assistant" && hasCode ? (
                    <div className="prose dark:prose-invert max-w-none">
                      {extractCodeBlocks(message.content)
                        .filter((block) => !block.isCode)
                        .map((block, idx) => (
                          <div key={idx} dangerouslySetInnerHTML={{ __html: marked(block.content) }} />
                        ))}
                    </div>
                  ) : (
                    renderMessage(message.content)
                  )}
                </motion.div>
              ))}
            </AnimatePresence>

            <AnimatePresence>
              {streamingMessage && (
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0 }}
                  className="bg-gradient-to-r from-gray-800 to-gray-700 p-4 rounded-xl mr-auto max-w-[90%] text-white"
                >
                  {renderMessage(streamingMessage)}
                </motion.div>
              )}
            </AnimatePresence>

            <AnimatePresence>
              {isLoading && !streamingMessage && (
                <motion.div
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  className="bg-gradient-to-r from-gray-800 to-gray-700 p-4 rounded-xl mr-auto max-w-[90%] animate-pulse text-white"
                >
                  Thinking...
                </motion.div>
              )}
            </AnimatePresence>
          </motion.div>

          <AnimatePresence>
            {hasCode && latestCodeBlock && (
              <motion.div
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: 20 }}
                className="h-[calc(100vh-12rem)] bg-gradient-to-br from-purple-900/30 to-pink-900/30 rounded-xl overflow-hidden sticky top-4 backdrop-blur-sm border border-purple-500/20"
              >
                <div className="flex border-b border-purple-500/20">
                  <button
                    onClick={() => setActiveTab("code")}
                    className={`flex-1 p-4 text-center ${activeTab === "code" ? "bg-purple-900/50 text-white" : "bg-transparent text-gray-400"}`}
                  >
                    Code View
                  </button>
                  <button
                    onClick={() => setActiveTab("scene")}
                    className={`flex-1 p-4 text-center ${activeTab === "scene" ? "bg-purple-900/50 text-white" : "bg-transparent text-gray-400"}`}
                  >
                    Scene View
                  </button>
                </div>
                <div className="h-full overflow-auto">
                  {activeTab === "code" && latestCodeBlock && (
                    <CodeView code={latestCodeBlock.code} language={latestCodeBlock.language} />
                  )}
                  {activeTab === "scene" && latestCodeBlock && (
                    <Suspense fallback={<div className="text-white p-4">Loading 3D Scene...</div>}>
                      <div className="w-full h-[400px]">
                        <SceneView code={latestCodeBlock.code} />
                      </div>
                    </Suspense>
                  )}
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        <motion.form
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          onSubmit={(e) => {
            e.preventDefault()
            handleSendMessage(input)
          }}
          className="fixed bottom-0 left-0 right-0 p-4 bg-gradient-to-r from-purple-900/80 to-pink-900/80 border-t border-purple-500/20 backdrop-blur-sm"
        >
          <div className="max-w-6xl mx-auto flex gap-2">
            <input
              type="text"
              value={input}
              onChange={(e) => setInput(e.target.value)}
              className="flex-1 p-2 border rounded-full focus:ring-2 focus:ring-purple-500 outline-none bg-gray-900/50 text-white border-purple-500/20"
              placeholder="Type your message..."
              aria-label="Chat message"
            />
            <button
              type="submit"
              className="px-4 py-2 bg-gradient-to-r from-purple-600 to-pink-600 text-white rounded-full hover:from-purple-700 hover:to-pink-700 disabled:opacity-50 disabled:hover:from-purple-600 disabled:hover:to-pink-600"
              disabled={isLoading || !input.trim()}
              aria-label="Send message"
            >
              Send
            </button>
          </div>
        </motion.form>
      </div>
    </main>
  )
}

// Wrap the entire page component with dynamic import to disable SSR
const ChatPage = dynamic(
  () =>
    Promise.resolve(() => (
      <Suspense
        fallback={
          <div className="min-h-screen bg-black flex items-center justify-center">
            <div className="text-white">Loading...</div>
          </div>
        }
      >
        <ChatPageContent />
      </Suspense>
    )),
  { ssr: false },
)

export default ChatPage

