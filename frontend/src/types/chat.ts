export type Message = {
  role: 'user' | 'assistant'
  content: string
}

export type ChatTemplate = {
  id: string
  title: string
  description: string
  icon: string
  initialMessage: string
} 