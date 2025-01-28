import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ScrollArea, ScrollBar } from "@/components/ui/scroll-area";
import {
  Battery,
  BoltIcon,
  Calendar,
  Home,
  LineChart,
  ParkingMeterIcon as Meter,
  Phone,
  Power,
  Wrench,
} from "lucide-react";
import Image from "next/image";

export default function AgentScheduler() {
  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold text-center mb-8">
        Choose an AI Agent to Get Started
      </h1>

      {/* Filter Buttons */}
      <div className="flex justify-center gap-2 mb-8">
        <Button variant="default" className="bg-green-500 hover:bg-green-600">
          All
        </Button>
        <Button
          variant="outline"
          className="border-green-500 text-green-500 hover:bg-green-50"
        >
          Residential
        </Button>
        <Button
          variant="outline"
          className="border-green-500 text-green-500 hover:bg-green-50"
        >
          Commercial
        </Button>
        <Button
          variant="outline"
          className="border-green-500 text-green-500 hover:bg-green-50"
        >
          Billing
        </Button>
        <Button
          variant="outline"
          className="border-green-500 text-green-500 hover:bg-green-50"
        >
          Technical
        </Button>
      </div>

      {/* Service Categories */}
      <ScrollArea className="w-full whitespace-nowrap mb-8">
        <div className="flex w-max space-x-8 p-4">
          <CategoryButton icon={<Home />} label="All" isActive />
          <CategoryButton icon={<Phone />} label="Billing Support" />
          <CategoryButton icon={<Meter />} label="Meter Reading" />
          <CategoryButton icon={<Power />} label="Power Outage" />
          <CategoryButton icon={<LineChart />} label="Usage Analysis" />
          <CategoryButton icon={<Battery />} label="Energy Storage" />
          <CategoryButton icon={<BoltIcon />} label="Solar Setup" />
          <CategoryButton icon={<Calendar />} label="Maintenance" />
          <CategoryButton icon={<Wrench />} label="Repairs" />
        </div>
        <ScrollBar orientation="horizontal" />
      </ScrollArea>

      {/* Agent Cards Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
        <AgentCard
          name="Alex"
          title="Billing and Payment Support"
          tags={["Billing", "Check-in"]}
          isNew
        />
        <AgentCard
          name="Sarah"
          title="Smart Meter Installation Consultation"
          tags={["Technical", "Installation"]}
        />
        <VideoCard />
        <AgentCard
          name="Michael"
          title="Energy Usage Analysis"
          tags={["Analysis", "Consultation"]}
          isNew
        />
        <AgentCard
          name="Emma"
          title="Solar Panel Setup Guide"
          tags={["Technical", "Solar"]}
        />
        <AgentCard
          name="David"
          title="Power Outage Response"
          tags={["Emergency", "Support"]}
        />
      </div>
    </div>
  );
}

function CategoryButton({ icon, label, isActive = false }) {
  return (
    <Button
      variant="ghost"
      className={`flex flex-col items-center gap-2 p-2 ${
        isActive ? "border-b-2 border-green-500" : ""
      }`}
    >
      <div className="w-12 h-12 flex items-center justify-center rounded-full bg-gray-100">
        {icon}
      </div>
      <span className="text-sm">{label}</span>
    </Button>
  );
}

function AgentCard({ name, title, tags, isNew = false }) {
  return (
    <Card className="overflow-hidden">
      <div className="relative">
        <Image
          src="/placeholder.svg?height=200&width=400"
          width={400}
          height={200}
          alt={name}
          className="w-full object-cover"
        />
        <div className="absolute top-4 right-4 flex gap-2">
          {isNew && (
            <span className="bg-green-500 text-white text-xs px-2 py-1 rounded">
              New
            </span>
          )}
          <span className="bg-blue-500 text-white text-xs px-2 py-1 rounded">
            AI
          </span>
        </div>
      </div>
      <div className="p-4">
        <h3 className="font-semibold text-lg">{name}</h3>
        <p className="text-green-500 mb-3">{title}</p>
        <div className="flex gap-2">
          {tags.map((tag) => (
            <span
              key={tag}
              className="text-xs border border-green-500 text-green-500 rounded-full px-3 py-1"
            >
              {tag}
            </span>
          ))}
        </div>
        <div className="mt-4 flex items-center gap-2">
          <div className="w-8 h-8 bg-gray-200 rounded-full flex items-center justify-center">
            <Power className="w-4 h-4" />
          </div>
          <div className="text-sm text-gray-500">
            <div>Created by:</div>
            <div>EnergyAI Assistant</div>
          </div>
        </div>
      </div>
    </Card>
  );
}

function VideoCard() {
  return (
    <Card className="overflow-hidden bg-gradient-to-br from-blue-600 to-blue-800 text-white flex items-center justify-center min-h-[400px]">
      <div className="text-center p-8">
        <h2 className="text-2xl font-bold mb-4">
          Hear our Energy AI Agents in Action
        </h2>
        <Button
          variant="outline"
          className="rounded-full w-16 h-16 border-2"
          size="icon"
        >
          <Power className="w-8 h-8" />
        </Button>
      </div>
    </Card>
  );
}
