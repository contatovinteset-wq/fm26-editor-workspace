import { getYoutubeLatestVideo } from './server/services/youtubeService.js';

(async () => {
  try {
    const video = await getYoutubeLatestVideo();
    console.log('LATEST_VIDEO_FOUND:', JSON.stringify(video, null, 2));
  } catch (err) {
    console.error('ERROR:', err);
  }
})();
