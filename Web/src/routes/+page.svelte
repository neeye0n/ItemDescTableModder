<script lang="ts">
	import '../app.css';
	import { base } from "$app/paths";
	
	let file: File | null = null;
	let fileInput: HTMLInputElement | null = null;
	let isDragOver = false;

	function handleDrop(event: DragEvent) {
		event.preventDefault();
		isDragOver = false;
		if (file) {
			showToast('Another file has been alread uploaded ⚠️', 'warning');
			return;
		}

		if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
			const droppedFile = event.dataTransfer.files[0];
			// Validate file type if needed
			if (droppedFile.name.endsWith('.lua')) {
				file = droppedFile;
				showToast('File uploaded successfully ✅', 'success');
			} else {
				showToast('Please select a .lua file ❌', 'error');
			}
		}
	}

	function handleDragOver(event: DragEvent) {
		event.preventDefault();
		isDragOver = true;
	}

	function handleDragLeave(event: DragEvent) {
		event.preventDefault();
		// Only set isDragOver to false if we're actually leaving the dropzone
		const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
		const x = event.clientX;
		const y = event.clientY;

		if (x < rect.left || x > rect.right || y < rect.top || y > rect.bottom) {
			isDragOver = false;
		}
	}

	function handleFileChanges(event: Event) {
		const input = event.target as HTMLInputElement;
		if (input.files && input.files.length > 0) {
			file = input.files[0];
			showToast('File uploaded successfully ✅', 'success');
		} else {
			file = null;
		}
	}

	function clear() {
		file = null;
	}

	function triggerFileInput() {
		if (file) {
			return;
		}
		fileInput?.click();
	}

	function generate(event: Event) {
		event.preventDefault();
		if (!file) {
			showToast('Select the file first ⚠️', 'warning');
			return;
		}

		//
		showToast('Success ✅', 'success');
		setTimeout(() => {
			toastMessage = null;
			clear();
		}, 1500);
	}

	let toastMessage: string | null = null;
	let toastType: 'info' | 'success' | 'warning' | 'error';

	function showToast(message: string, type: 'info' | 'success' | 'warning' | 'error') {
		toastMessage = message;
		toastType = type;

		// Auto-hide after 3 seconds
		setTimeout(() => {
			toastMessage = null;
		}, 3000);
	}
</script>

<div class="bg-base-200 flex min-h-screen items-center justify-center">
	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<!-- svelte-ignore a11y_click_events_have_key_events -->
	<div
		class="card bg-base-100 w-full max-w-xs cursor-pointer border-2 border-dashed shadow-xl transition-all duration-200
		       {isDragOver
			? 'border-primary bg-primary/10'
			: file
				? 'border-success'
				: 'border-base-300 hover:border-primary/50'}"
		on:drop={handleDrop}
		on:dragover={handleDragOver}
		on:dragleave={handleDragLeave}
		on:click={triggerFileInput}
	>
		<div class="card-body space-y-4">
			<div class="flex items-center justify-between">
				<h2 class="card-title">ItemDescTableModder</h2>
				<div
					class="lg:tooltip md:tooltip"
					data-tip="A simple tool for customizing item descriptions on itemInfo_EN.lua for Ragnarok Online."
				>
					<img src="{base}/noted.gif" alt="Logo" class="h-15 w-15 rounded-full object-cover" />
				</div>
			</div>

			<fieldset class="fieldset">
				<div class="py-4 text-center">
					{#if isDragOver}
						<div class="text-primary">
							<svg
								class="mx-auto mb-2 h-12 w-12"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
								/>
							</svg>
							<p class="font-medium">Release to upload</p>
						</div>
					{:else if file}
						<div class="text-success">
							<svg
								class="mx-auto mb-2 h-12 w-12"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
								/>
							</svg>
							<p class="font-medium">File ready!</p>
						</div>
					{:else}
						<div class="text-base-content/60">
							<svg
								class="mx-auto mb-2 h-12 w-12"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M9 13h6m-3-3v6m5 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
								/>
							</svg>
							<p class="font-medium">Click or drag here to upload</p>
						</div>
					{/if}
				</div>

				<input
					type="file"
					accept=".lua"
					class="file-input hidden w-full"
					bind:this={fileInput}
					on:change={handleFileChanges}
				/>

				{#if file}
					<div class="mt-2">
						<div class="alert alert-success">
							<svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
								/>
							</svg>
							<div>
								<h4 class="font-bold">{file.name}</h4>
								<div class="text-xs">{(file.size / 1024).toFixed(1)} KB</div>
							</div>
							<svg
								class="h-6 w-6"
								xmlns="http://www.w3.org/2000/svg"
								fill="none"
								viewBox="0 0 24 24"
								stroke="currentColor"
								on:click|stopPropagation={clear}
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M6 18L18 6M6 6l12 12"
								/>
							</svg>
						</div>
					</div>
				{:else}
					<p class="text-info text-center text-xs">Where is your itemInfo_EN.lua file? 👀</p>
				{/if}
			</fieldset>

			<div class="card-actions justify-end">
				<button class="btn btn-primary w-full" class:btn-disabled={!file} on:click={generate}>
					{file ? 'Make it happen!' : 'Select a file first!'}
				</button>
			</div>
		</div>
	</div>

	{#if toastMessage}
		<div class="toast toast-top toast-center z-50">
			<div
				class={`alert ${toastType === 'success' ? 'alert-success' : toastType === 'error' ? 'alert-error' : toastType === 'warning' ? 'alert-warning' : 'alert-info'}`}
				role="alert"
			>
				<span>{toastMessage}</span>
			</div>
		</div>
	{/if}
</div>
