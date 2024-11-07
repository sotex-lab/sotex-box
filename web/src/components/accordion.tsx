"use client"
import { useState } from "react";
import Image from "next/image";

type AccordionData = {
        list: AccordionItem[]
        header: string
}

type AccordionItem = {
        question: string,
        answer: string
}

export function AccordionItem({ pair }: { pair: AccordionItem }) {
        const [expanded, setExpanded] = useState<boolean>(false);

        const chevronRotation = expanded ? "rotate-180" : "";
        const textVisible = expanded ? "" : "max-h-0 opacity-0 overflow-hidden";

        return (
                <div className="w-full p-2 border-b-sotex-black-450 border-b-[1px] cursor-pointer" onClick={() => { setExpanded(!expanded) }}>
                        <div className="flex flex-row justify-between items-center w-full">
                                <p className="text-2xl text-white">{pair.question}</p>
                                <Image alt="chevron up" className={`transition ease-in-out ${chevronRotation}`} src={"/assets/icons/chevron-down.svg"} width={24} height={24}></Image>
                        </div>
                        <div className={`transition-opacity ease-in-out duration-300 ${textVisible}`}>
                                <p className={`text-lg text-slate-300 ${textVisible}`} > {pair.answer}</p>
                        </div>
                </div >
        );

}

export default function Accordion({ data }: { data: AccordionData }) {
        return (<section className="bg-sotex-black-50 py-12 px-4">
                <div className="flex flex-col justify-center items-start max-w-5xl m-auto lg:items-start">
                        <h2 className="text-4xl text-white font-medium py-6 px-2">{data.header}</h2>
                        <div className="w-full flex flex-col gap-4">
                                {data.list.map((item: AccordionItem) => {
                                        return <AccordionItem pair={item} key={item.answer + item.question} />
                                })
                                }
                        </div>
                </div>
        </section>);


}


export function LandingAccordion() {
        const data: AccordionData = {
                header: "Često postavljena pitanja",
                list: [
                        {
                                question: "Kome je ovo namenjeno",
                                answer: "Vi na kraju procesa dobijate izveštaj i statistiku o svakom polazniku.Svaki polaznik stice sertifikat o polozenoj obuci za bezbednost na internetu. Vi na kraju procesa dobijate izveštaj i statistiku o svakom polazniku.Svaki polaznik stice sertifikat o polozenoj obuci za bezbednost na internetu. Vi na kraju procesa dobijate izveštaj i statistiku o svakom polazniku."
                        },
                        {
                                question: "Kome je ovo namenjeno",
                                answer: "Svaki polaznik stice sertifikat o polozenoj obuci za bezbednost na internetu. "
                        },
                        {
                                question: "Kome je ovo namenjeno",
                                answer: "Vi na kraju procesa dobijate izveštaj i statistiku o svakom polazniku.Svaki polaznik stice sertifikat o polozenoj obuci za bezbednost na internetu. Vi na kraju procesa dobijate izveštaj i statistiku o svakom polazniku.Svaki polaznik stice sertifikat o polozenoj obuci za bezbednost na internetu. Vi na kraju procesa dobijate izveštaj i statistiku o svakom polazniku."
                        },
                ]
        }

        return <Accordion data={data}></Accordion>
}
