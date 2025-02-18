"use client"

import type React from "react"
import { useEffect, useRef, useState } from "react"
import * as THREE from "three"
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls"
import { GLTFLoader } from "three/examples/jsm/loaders/GLTFLoader"
import { OBJLoader } from "three/examples/jsm/loaders/OBJLoader"
import { FBXLoader } from "three/examples/jsm/loaders/FBXLoader"
import TWEEN from "@tweenjs/tween.js"

interface SceneViewProps {
  code: string
}

const SceneView: React.FC<SceneViewProps> = ({ code }) => {
  const containerRef = useRef<HTMLDivElement>(null)
  const [error, setError] = useState<string | null>(null)
  const [debugInfo, setDebugInfo] = useState<string>("")
  const [uploadedFile, setUploadedFile] = useState<File | null>(null)

  // Persistent refs for scene, camera, renderer, and controls
  const sceneRef = useRef<THREE.Scene | null>(null)
  const cameraRef = useRef<THREE.PerspectiveCamera | null>(null)
  const rendererRef = useRef<THREE.WebGLRenderer | null>(null)
  const controlsRef = useRef<OrbitControls | null>(null)

  useEffect(() => {
    if (!containerRef.current) return

    // Create the scene, camera, renderer, and controls once
    if (!sceneRef.current) {
      sceneRef.current = new THREE.Scene()
      cameraRef.current = new THREE.PerspectiveCamera(
        75,
        containerRef.current.clientWidth / containerRef.current.clientHeight,
        0.1,
        1000,
      )
      rendererRef.current = new THREE.WebGLRenderer({ antialias: true })
      rendererRef.current.setSize(containerRef.current.clientWidth, containerRef.current.clientHeight)
      containerRef.current.appendChild(rendererRef.current.domElement)

      // Set initial camera position for better 3D viewing
      cameraRef.current.position.set(0, 0, 5)
      cameraRef.current.lookAt(0, 0, 0)

      // Add OrbitControls
      controlsRef.current = new OrbitControls(cameraRef.current, rendererRef.current.domElement)
      controlsRef.current.enableDamping = true
      controlsRef.current.dampingFactor = 0.25
    }

    const scene = sceneRef.current!
    const camera = cameraRef.current!
    const renderer = rendererRef.current!
    const controls = controlsRef.current!

    // Clear existing objects from the scene
    while (scene.children.length > 0) {
      scene.remove(scene.children[0])
    }

    // Add ambient light for better 3D visibility
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.5)
    scene.add(ambientLight)

    // Add directional light for 3D shading
    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.5)
    directionalLight.position.set(5, 5, 5)
    scene.add(directionalLight)

    const addDebugInfo = (info: string) => {
      setDebugInfo((prev) => prev + info + "\n")
    }

    // Define a generic helper function for create-or-update logic
    function updateOrCreate(name: string, createFn: () => THREE.Object3D, updateFn: (object: THREE.Object3D) => void) {
      let object = scene.getObjectByName(name)
      if (object) {
        updateFn(object)
      } else {
        object = createFn()
        if (!object) {
          console.error("Error: createFn did not return a valid object for", name)
          return
        }
        object.name = name
        scene.add(object)
      }
      return object
    }

    // Helper function to load 3D models
    const loadModel = (url: string) => {
      return new Promise((resolve, reject) => {
        const loader = new GLTFLoader()
        loader.load(
          url,
          (gltf) => {
            resolve(gltf.scene)
          },
          undefined,
          (error) => {
            reject(error)
          },
        )
      })
    }

    // Helper function to load uploaded file
    const loadUploadedFile = (file: File) => {
      return new Promise((resolve, reject) => {
        const reader = new FileReader()
        reader.onload = (event) => {
          if (!event.target?.result) return reject(new Error("Failed to read file"))
          
          const contents = event.target.result
          const fileExtension = file.name.split(".").pop()?.toLowerCase()

          try {
            if (fileExtension === "obj") {
              // OBJ files are text-based
              if (typeof contents !== "string") {
                throw new Error("Invalid content type for OBJ file")
              }
              const loader = new OBJLoader()
              const object = loader.parse(contents)
              resolve(object)
            } else if (fileExtension === "fbx") {
              // FBX files are binary
              if (!(contents instanceof ArrayBuffer)) {
                throw new Error("Invalid content type for FBX file")
              }
              const loader = new FBXLoader()
              const object = loader.parse(contents, '')
              resolve(object)
            } else {
              reject(new Error("Unsupported file type"))
            }
          } catch (error) {
            reject(error)
          }
        }
        reader.onerror = (error) => reject(error)

        // Use appropriate reader method based on file type
        if (file.name.toLowerCase().endsWith(".obj")) {
          reader.readAsText(file)  // Read OBJ files as text
        } else {
          reader.readAsArrayBuffer(file)  // Read FBX files as binary
        }
      })
    }

    // Execute the injected code with context that includes the helpers
    try {
      const executeCode = new Function(
        "THREE",
        "scene",
        "camera",
        "renderer",
        "controls",
        "updateOrCreate",
        "addDebugInfo",
        "loadModel",
        "TWEEN",
        `
        try {
          ${code}
          //addDebugInfo("Custom code executed successfully");
        } catch (error) {
          addDebugInfo("Error executing custom code: " + error.message);
          throw error;
        }
      `,
      )

      executeCode(THREE, scene, camera, renderer, controls, updateOrCreate, addDebugInfo, loadModel, TWEEN)
    } catch (error: unknown) {
      console.error("Error executing WebGL code:", error)
      if (error instanceof Error) {
        setError(error.message)
      } else {
        setError("An unknown error occurred")
      }
    }

    // Load uploaded file if present
    if (uploadedFile) {
      loadUploadedFile(uploadedFile)
        .then((object) => {
          const loadedObject = object as THREE.Object3D
          scene.add(loadedObject)

          // Center and scale the loaded object
          const box = new THREE.Box3().setFromObject(loadedObject)
          const center = box.getCenter(new THREE.Vector3())
          const size = box.getSize(new THREE.Vector3())

          const maxDim = Math.max(size.x, size.y, size.z)
          const scale = 5 / maxDim // Scale to fit in a 5x5x5 cube
          loadedObject.scale.multiplyScalar(scale)

          loadedObject.position.sub(center.multiplyScalar(scale))

          camera.position.set(0, 0, 10)
          camera.lookAt(0, 0, 0)
          controls.update()

          //addDebugInfo("File loaded successfully")
        })
        .catch((error: unknown) => {
          console.error("Error loading file:", error)
          if (error instanceof Error) {
            setError(`Error loading file: ${error.message}`)
          } else {
            setError("An unknown error occurred while loading the file")
          }
        })
    }

    // Animation loop
    const animate = () => {
      requestAnimationFrame(animate)
      TWEEN.update()
      controls.update()
      renderer.render(scene, camera)
    }
    animate()

    // Handle window resize
    const handleResize = () => {
      if (containerRef.current) {
        const width = containerRef.current.clientWidth
        const height = containerRef.current.clientHeight
        camera.aspect = width / height
        camera.updateProjectionMatrix()
        renderer.setSize(width, height)
      }
    }

    window.addEventListener("resize", handleResize)

    return () => {
      window.removeEventListener("resize", handleResize)
    }
  }, [code, uploadedFile])

  const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (file) {
      setUploadedFile(file)
    }
  }

  if (error) {
    return (
      <div className="text-red-500 p-4">
        <p>Error: {error}</p>
        <pre className="mt-4 p-2 bg-gray-800 text-white rounded">{debugInfo}</pre>
      </div>
    )
  }

  return (
    <div className="relative w-full h-full">
      <div ref={containerRef} className="w-full h-full bg-black rounded-xl" />
      {debugInfo && (
        <pre className="absolute top-0 left-0 p-2 bg-black bg-opacity-50 text-white text-xs">{debugInfo}</pre>
      )}
      <div className="absolute bottom-4 left-4">
        <input type="file" accept=".obj,.fbx" onChange={handleFileUpload} className="hidden" id="file-upload" />
        <label
          htmlFor="file-upload"
          className="bg-gradient-to-r from-purple-600 to-pink-600 hover:from-purple-700 hover:to-pink-700 text-white font-bold py-2 px-4 rounded-full cursor-pointer transition-colors duration-300"
        >
          Upload 3D Model
        </label>
      </div>
    </div>
  )
}

export default SceneView

