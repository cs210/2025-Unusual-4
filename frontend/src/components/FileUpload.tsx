"use client"

import type React from "react"
import { useState } from "react"
import { Upload } from "lucide-react"

interface FileUploadProps {
  onFileUpload: (file: File) => void
}

const FileUpload: React.FC<FileUploadProps> = ({ onFileUpload }) => {
  const [dragActive, setDragActive] = useState(false)

  const handleDrag = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (e.type === "dragenter" || e.type === "dragover") {
      setDragActive(true)
    } else if (e.type === "dragleave") {
      setDragActive(false)
    }
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setDragActive(false)
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      onFileUpload(e.dataTransfer.files[0])
    }
  }

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    e.preventDefault()
    if (e.target.files && e.target.files[0]) {
      onFileUpload(e.target.files[0])
    }
  }

  return (
    <div
      className={`inline-flex items-center justify-center ${
        dragActive ? "bg-purple-600" : "bg-gradient-to-r from-purple-600 to-pink-600"
      } rounded-full p-2 cursor-pointer transition-colors duration-300 hover:from-purple-700 hover:to-pink-700`}
      onDragEnter={handleDrag}
      onDragLeave={handleDrag}
      onDragOver={handleDrag}
      onDrop={handleDrop}
    >
      <label htmlFor="dropzone-file" className="flex items-center justify-center cursor-pointer">
        <div className="flex items-center space-x-2 text-white">
          <Upload className="w-4 h-4" />
          <span className="text-xs font-medium">Upload 3D</span>
        </div>
        <input id="dropzone-file" type="file" className="hidden" accept=".obj,.fbx" onChange={handleChange} />
      </label>
    </div>
  )
}

export default FileUpload

