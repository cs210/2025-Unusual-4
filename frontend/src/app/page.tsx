import QuestionInput from '@/components/QuestionInput'
import ChatTemplates from '@/components/ChatTemplates'
import ChatHistory from '@/components/ChatHistory'

export default function Home() {
  return (
    <main className="min-h-screen p-8 max-w-4xl mx-auto">
      <div className="space-y-8">
        <h1 className="text-4xl font-bold text-center">AI Chat Assistant</h1>
        
        <QuestionInput />
        
        <ChatHistory />
        
        <div className="mt-12">
          <h2 className="text-2xl font-semibold mb-6">Chat Templates</h2>
          <ChatTemplates />
        </div>
      </div>
    </main>
  )
} 